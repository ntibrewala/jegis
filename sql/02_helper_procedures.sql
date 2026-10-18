-- ============================================================================
-- 02: HELPER STORED PROCEDURES (JEGIS)
-- Execute this script in the target SAP HANA company schema
-- ============================================================================

-- Procedure 1: TEC_COMBINED_TOKEN_GET
-- Fetches GSTIN, API Credentials, and Cached Token for a given document
CREATE OR REPLACE PROCEDURE "TEC_COMBINED_TOKEN_GET"
(
    IN ObjType VARCHAR(20),
    IN DocEntry VARCHAR(25)
)
LANGUAGE SQLSCRIPT AS
BEGIN
    DECLARE v_whs NVARCHAR(50) := '';
    DECLARE v_state NVARCHAR(10) := '';
    DECLARE v_gstin NVARCHAR(20) := '';

    -- 1. Identify Warehouse from document lines
    IF :ObjType IN ('13', 'Invoice') THEN
        SELECT MAX("WhsCode") INTO v_whs FROM "INV1" WHERE "DocEntry" = CAST(:DocEntry AS INTEGER);
    ELSEIF :ObjType IN ('14', 'CreditMemo') THEN
        SELECT MAX("WhsCode") INTO v_whs FROM "RIN1" WHERE "DocEntry" = CAST(:DocEntry AS INTEGER);
    ELSEIF :ObjType IN ('67', 'Transfer') THEN
        SELECT MAX("WhsCode") INTO v_whs FROM "WTR1" WHERE "DocEntry" = CAST(:DocEntry AS INTEGER);
    END IF;

    -- 2. Derive GST State code from Warehouse Location
    IF IFNULL(:v_whs, '') <> '' THEN
        SELECT MAX(T1."GSTCode") INTO v_state
        FROM "OWHS" T2
        INNER JOIN "OLCT" T0 ON T0."Code" = T2."Location"
        INNER JOIN "OCST" T1 ON T0."State" = T1."Code" AND T1."Country" = 'IN'
        WHERE T2."WhsCode" = :v_whs;
    END IF;

    -- 3. Match State code with State-wise GSTIN setup
    IF IFNULL(:v_state, '') <> '' THEN
        SELECT MAX("U_GSTIN") INTO v_gstin FROM "@GIS_EI_OGST" WHERE "Name" = :v_state;
    END IF;

    -- 4. Return Credentials & Active Cached Token
    IF IFNULL(:v_gstin, '') <> '' THEN
        SELECT TOP 1 
            A."U_GSTIN" AS "GSTIN",
            A."U_AspID" AS "AspId",
            A."U_AspPwd" AS "AspPassword",
            A."U_UserName" AS "UserName",
            A."U_Password" AS "ApiPassword",
            A."U_CltID" AS "ClientId",
            A."U_CltSecrt" AS "ClientSecret",
            T."AuthToken",
            T."TokenExpiry"
        FROM "@GIS_EI_OGST" A
        LEFT JOIN "TEC_AUTHTOKEN" T 
            ON A."U_GSTIN" = T."GSTIN" 
            AND T."Type" = 'E-Invoice' 
            AND T."TokenExpiry" > CURRENT_TIMESTAMP
        WHERE A."U_GSTIN" = :v_gstin;
    ELSE
        SELECT TOP 1 
            A."U_GSTIN" AS "GSTIN",
            A."U_AspID" AS "AspId",
            A."U_AspPwd" AS "AspPassword",
            A."U_UserName" AS "UserName",
            A."U_Password" AS "ApiPassword",
            A."U_CltID" AS "ClientId",
            A."U_CltSecrt" AS "ClientSecret",
            T."AuthToken",
            T."TokenExpiry"
        FROM "@GIS_EI_OGST" A
        LEFT JOIN "TEC_AUTHTOKEN" T 
            ON A."U_GSTIN" = T."GSTIN" 
            AND T."Type" = 'E-Invoice' 
            AND T."TokenExpiry" > CURRENT_TIMESTAMP;
    END IF;
END;


-- Procedure 2: TEC_GETWHSANDSTATE
-- Resolves Warehouse Code or State Code for document processing
CREATE OR REPLACE PROCEDURE "TEC_GETWHSANDSTATE"
(
    IN DocEntry NVARCHAR(50),
    IN ObjType NVARCHAR(20),
    IN DType NVARCHAR(20)
)
LANGUAGE SQLSCRIPT AS
BEGIN
    IF :DType = 'WHS' THEN
        IF :ObjType IN ('13', 'Invoice') THEN
            SELECT MAX("WhsCode") AS "WhsCode" FROM "INV1" WHERE "DocEntry" = CAST(:DocEntry AS INTEGER);
        ELSEIF :ObjType IN ('14', 'CreditMemo') THEN
            SELECT MAX("WhsCode") AS "WhsCode" FROM "RIN1" WHERE "DocEntry" = CAST(:DocEntry AS INTEGER);
        ELSEIF :ObjType IN ('67', 'Transfer') THEN
            SELECT MAX("WhsCode") AS "WhsCode" FROM "WTR1" WHERE "DocEntry" = CAST(:DocEntry AS INTEGER);
        END IF;
    ELSEIF :DType = 'State' THEN
        -- When DType='State', DocEntry contains the WhsCode
        SELECT MAX(T1."GSTCode") AS "StateCode"
        FROM "OWHS" T0
        INNER JOIN "OLCT" D ON D."Code" = T0."Location"
        INNER JOIN "OCST" T1 ON T1."Code" = D."State" AND T1."Country" = D."Country"
        WHERE T0."WhsCode" = :DocEntry;
    END IF;
END;


-- Procedure 3: TEC_EWAYLoginURL
-- Resolves the target Chartered API URL for E-Way Bill operations
CREATE OR REPLACE PROCEDURE "TEC_EWAYLoginURL"
(
    IN DType NVARCHAR(50),
    IN State NVARCHAR(50)
)
LANGUAGE SQLSCRIPT AS
BEGIN
    SELECT TOP 1 "U_Url" AS "URL" 
    FROM "@GIS_EW_OAPI" 
    WHERE "U_Action" = :DType OR "Code" = :DType OR "Name" = :DType;
END;
