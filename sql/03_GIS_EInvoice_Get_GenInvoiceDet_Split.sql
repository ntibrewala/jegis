CREATE OR REPLACE PROCEDURE "GIS_EInvoice_Get_GenInvoiceDet_Split"
(IN ObjType varchar(20), IN DocEntry varchar(25), IN ReqType varchar(30),IN DType nvarchar(20))
AS
DocType nvarchar(100);

--CALL "GIS_EInvoice_Get_GenInvoiceDet_Split" ('13','7286','Application','Header')

BEGIN
IF :ReqType = 'Addon' or  :ReqType = 'Application' THEN
SELECT (SELECT :DocEntry from dummy) INTO DocEntry FROM DUMMY;
SELECT (SELECT :ObjType FROM dummy) INTO DocType FROM DUMMY;
END IF;
--This procedure is used to get invoice details to generate E-Invoice
-- Header Details

    
IF :DType='Header'  THEN

create local temporary table "#HdrDet"("ObjType" varchar(50),"Version" nvarchar(10),"TaxSch" nvarchar(50),
    "RegRev" nvarchar(5),"IgstOnIntra" nvarchar(5),"DocEntry" nvarchar(25),
    "SupTyp" nvarchar(50),"Type" nvarchar(50),"DocNum" varchar(20),"Dt" nvarchar(10),"Seller_GSTIN" nvarchar(30),
    "Seller_Trade_Name" nvarchar(100),
    "Seller_Addr1" nvarchar(100),"Seller_Addr2" nvarchar(100),"Seller_Loc" nvarchar(100),"Seller_Pin" nvarchar(25),"Seller_State" nvarchar(50),
    "Seller_Ph" nvarchar(500),"Seller_Em" nvarchar(100),"Buyer_GSTIN" nvarchar(30),"Buyer_LglNm" nvarchar(100),"Buyer_TrdNm" nvarchar(100),
    "Buyer_Pos" nvarchar(100),"Buyer_Addr1"  nvarchar(100),"Buyer_Addr2"  nvarchar(100),"Buyer_Loc" nvarchar(100),"Buyer_Pin" nvarchar(25),"Buyer_State" nvarchar(50),
    "Buyer_Ph" nvarchar(500),"Buyer_Em" nvarchar(100),"Disp_Dtl_Nm" nvarchar(100),"Disp_Dtl_Addr1" nvarchar(100),"Disp_Dtl_Addr2" nvarchar(100),"Disp_Dtl_Loc" nvarchar(100),
    "Disp_Dtl_Pin" nvarchar(25),"Disp_Dtl_Stcd" nvarchar(50),"ShipDtls_GSTIN" nvarchar(100),"ShipDtls_LglNm" nvarchar(100),"ShipDtls_TrdNm" nvarchar(100),"ShipDtls_Addr1" nvarchar(100),
    "ShipDtls_Addr2" nvarchar(100),"ShipDtls_Loc" nvarchar(100),"ShipDtls_Pin" nvarchar(25),"ShipDtls_Stcd" nvarchar(50),"AssVal" varchar(100),"SgstVal" varchar(100),"CgstVal" varchar(100),
    "IgstVal" varchar(100),"CesVal" varchar(100),"StCesVal" decimal(19,3),"RndOffAmt" varchar(100),"TotInvVal" varchar(100),"TCSVal" varchar(100),"DiscSum" varchar(100),"URL" varchar(100),
    "SAddress2" nvarchar(50),"SAddress3" nvarchar(50));
    
    a=    SELECT "AdresType", T0."Address", T0."CardCode",IFNUll(T0."Address2", T0."Street") "Street",
    ifnull("Address3", T0."Block") "Block", T0."ZipCode", T0."City", T0."County",
    T0."Country", T0."State", T0."GSTRegnNo" FROM CRD1 T0 
    WHERE :DocType='13' and T0."CardCode"=(Select "CardCode" from OINV where "DocEntry"=:DocEntry)
    union all
     SELECT "AdresType", T0."Address", T0."CardCode", T0."Street", T0."Block", T0."ZipCode", T0."City", T0."County",
    T0."Country", T0."State", T0."GSTRegnNo" FROM CRD1 T0 
    WHERE :DocType='14' and T0."CardCode"=(Select "CardCode" from ORIN where "DocEntry"=:DocEntry)
    union all
    SELECT "AdresType", T0."Address", T0."CardCode", T0."Street", T0."Block", T0."ZipCode", T0."City", T0."County",
    T0."Country", T0."State", T0."GSTRegnNo" FROM CRD1 T0 
    WHERE :DocType='19' and T0."CardCode"=(Select "CardCode" from ORPC where "DocEntry"=:DocEntry);
    
INSERT INTO "#HdrDet"
Select Distinct :ObjType AS "ObjType", '1.1' AS "Version",'GST' AS "TaxSch",'N' AS "RegRev",'N' AS "IgstOnIntra"
,:DocEntry as "DocEntry",'B2B' as "SupTyp" , 
case when t0."DocSubType"='GD' then 'DBN' else 'INV' end as "Type",
"SeriesName"||'/'||cast(T0."DocNum" as varchar(20))  as "DocNum",TO_VARCHAR(CAST(T0."DocDate" AS varchar(10)),'DD/MM/YYYY') as "Dt" ,
T4."GSTRegnNo" as "Seller_GSTIN",IFNULL((select Top 1 IFNULL("PrintHdrF","CompnyName") from OADM),'JAY EXTRUTECH') as "Seller_Trade_Name",
--- seller

IFNULL(T4."Street" ,T2."Street" ) "Seller_Addr1",  
IFNULL(CAST(T4."Block" AS NVARCHAR(250)),T2."Block") as "Seller_Addr2",
--'Nashik' "Seller_Addr2",
IFNULL(T4."City",T2."City") as "Seller_Loc",
IFNULL(T4."ZipCode",T2."City") as "Seller_Pin",
IFNULL(T5."GSTCode",'') as "Seller_State",
IFNULL(T2."Address2",'9000310598') as "Seller_Ph" ,
IFNULL((SELECT "E_Mail" FROM OADM),'info@jayextrutech.com') as "Seller_Em",

---- Seller

--- buyer add= bill to address
T9."GSTRegnNo" as "Buyer_GSTIN",
T0."CardName" as "Buyer_LglNm",
T0."CardName" as "Buyer_TrdNm",
IFNULL(T3."BPStatGSTN",T7."GSTCode") as "Buyer_Pos",
T9."Street" as "Buyer_Addr1",
T9."Block" "Buyer_Addr2",
T9."City"  as "Buyer_Loc",
T9."ZipCode" "Buyer_Pin",
left(T9."GSTRegnNo",2)  as "Buyer_State",
CASE WHEN IFNULL(T8."Cellular",'') = '' THEN '9000310598' ELSE LEFT(IFNULL(T8."Cellular",''),10) END as "Buyer_Ph",
CASE WHEN IFNULL(T8."E_Mail",'test1@gmail.com')='' THEN 'test1@gmail.com' ELSE 'test1@gmail.com' END as "Buyer_Em",

--- Dispatch from = Warehouse address
IFNULL((select Top 1 IFNULL("PrintHdrF","CompnyName") from OADM),'JAY EXTRUTECH') as "Disp_Dtl_Nm",

IFNULL(CAST(T2."Street" AS NVARCHAR(250)),T4."Street" )  as "Disp_Dtl_Addr1",
IFNULL(CAST(T2."StreetNo" AS NVARCHAR(250)),T4."Block") ||',' || IFNULL(CAST(T2."Block" AS NVARCHAR(250)),T4."Building") as "Disp_Dtl_Addr2",
IFNULL(T2."City",T4."City")  as "Disp_Dtl_Loc",
IFNULL(T2."ZipCode",T4."ZipCode") "Disp_Dtl_Pin"
,CASE WHEN T0."DocType" = 'S' THEN T5."GSTCode" ELSE T11."GSTCode" END AS "Disp_Dtl_Stcd"
,T9."GSTRegnNo" as "ShipDtls_GSTIN",T0."CardName" as "ShipDtls_LglNm"
,T0."CardName"  as "ShipDtls_TrdNm",
---- dispatch to -- Ship to address
T10."Street" "ShipDtls_Addr1", T10."Block" as "ShipDtls_Addr2",
T10."City" "ShipDtls_Loc",
--'Telangana' "ShipDtls_Loc",
T10."ZipCode" as "ShipDtls_Pin",
left(T10."GSTRegnNo",2)   as "ShipDtls_Stcd",
--'29' "ShipDtls_Stcd",


(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM INV4 I1 WHERE I1."BaseSum">=0 and I1."staType" IN (-110,-120) AND I1."DocEntry" = T0."DocEntry" ) AS "AssVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM INV4 I1 WHERE I1."TaxSum">=0 and I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry" ) AS "SgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM INV4 I1 WHERE I1."TaxSum">=0 and I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry" ) AS "CgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM INV4 I1 WHERE I1."TaxSum">=0 and  I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" ) AS "IgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM INV4 I1 WHERE I1."TaxSum">=0 and  I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" ) AS "CesVal",
0.00 "StCesVal",
IFNULL(CAST(CAST(T0."RoundDif" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "RndOffAmt",

/*(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM INV4 I1 WHERE I1."staType" IN (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1')+
*/


IFNULL(CAST(CAST(T0."DocTotal" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "TotInvVal",
0 AS "TCSVal"
,IFNULL((Select SUM(M0."GTotal"*-1) from INV1 M0 where M0."DocEntry"=T0."DocEntry" and M0."Quantity"<0), '0.000')  as "DiscSum"  
,(SELECT "U_Url" FROM "@GIS_EI_OAPI" WHERE "U_Action"='GenerateEInvoice') as "URL",
(select  Replace(IFNULL(Max(M."Address2"),'ABC'),'"','')  from CRD1 M where M."CardCode"=T9."CardCode")"SAddress2",
(select  Replace(IFNULL(Max(M."Address3"),'ABC'),'"','')  from CRD1 M where M."CardCode"=T9."CardCode")"SAddress3"
--IFNULL(T9."Address3",'ABC') "SAddress3"
From OINV T0
inner JOIN INV1 T1 on T0."DocEntry" = T1."DocEntry" and T0."DocEntry" = :DocEntry
and  T0."CANCELED" ='N'
inner JOIN INV12 T3 on T3."DocEntry" = T0."DocEntry"
inner join NNM1 F0 on F0."Series"=T0."Series"
Inner JOIN OCRD T8 ON T8."CardCode" =T0."CardCode"
LEFT OUTER JOIN OWHS T2 on T2."WhsCode" = T1."WhsCode"
LEFT OUTER JOIN OLCT T4 on T4."Code" = T1."LocCode" 
Left Outer join OCST T5 on T5."Code" = T4."State" and T5."Country" ='IN'
Left Outer join OCST T6 on T6."Code" = T3."StateB" and T6."Country" = T3."CountryB"
Left Outer Join OCST T7 on T7."Code" = T3."StateS" and T7."Country" = T3."CountryS"
Left Outer Join OCST T11 on T11."Code" = T2."State" and T11."Country" = 'IN'
LEFT OUTER JOIN :a T9 ON T9."CardCode" =T0."CardCode" AND T9."AdresType" ='B' and T9."Address"=T0."PayToCode"  
LEFT OUTER JOIN :a T10 ON T10."CardCode" =T0."CardCode" AND T10."AdresType" ='S' and T10."Address"=T0."ShipToCode"
WHERE T0."DocEntry"=:DocEntry and :DocType ='13'


union all

Select Distinct :ObjType AS "ObjType", '1.1' AS "Version",'GST' AS "TaxSch",'N' AS "RegRev",'N' AS "IgstOnIntra"
,:DocEntry as "DocEntry",'B2B' as "SupTyp" ,'CRN' as "Type",
"SeriesName"||'/'||cast(T0."DocNum" as varchar(20))  as "DocNum",TO_VARCHAR(CAST(T0."DocDate" AS varchar(10)),'DD/MM/YYYY') as "Dt" ,
T4."GSTRegnNo" as "Seller_GSTIN",IFNULL((select Top 1 IFNULL("PrintHdrF","CompnyName") from OADM),'JAY EXTRUTECH') as "Seller_Trade_Name",
--- seller

IFNULL(T4."Street" ,T2."Street" ) "Seller_Addr1",  
IFNULL(CAST(T4."Block" AS NVARCHAR(250)),T2."StreetNo") as "Seller_Addr2",
IFNULL(T4."City",T2."City") as "Seller_Loc",
IFNULL(T4."ZipCode",T2."City") as "Seller_Pin",
IFNULL(T5."GSTCode",'') as "Seller_State",
IFNULL(T2."Address2",'9000310598') as "Seller_Ph" ,
IFNULL((SELECT "E_Mail" FROM OADM),'info@jayextrutech.com') as "Seller_Em",

---- Seller

--- buyer add= bill to address
T9."GSTRegnNo" as "Buyer_GSTIN",
T0."CardName" as "Buyer_LglNm",
T0."CardName" as "Buyer_TrdNm",
IFNULL(T3."BPStatGSTN",T7."GSTCode") as "Buyer_Pos",
T9."Street" as "Buyer_Addr1",
T9."Block" "Buyer_Addr2",
T9."City"  as "Buyer_Loc",
T9."ZipCode" "Buyer_Pin",
left(T9."GSTRegnNo",2)  as "Buyer_State",
CASE WHEN IFNULL(T8."Cellular",'') = '' THEN '9000310598' ELSE LEFT(IFNULL(T8."Cellular",''),10) END as "Buyer_Ph",
CASE WHEN IFNULL(T8."E_Mail",'test1@gmail.com')='' THEN 'test1@gmail.com' ELSE 'test1@gmail.com' END as "Buyer_Em",

--- Dispatch from = Warehouse address
IFNULL((select Top 1 IFNULL("PrintHdrF","CompnyName") from OADM),'JAY EXTRUTECH') as "Disp_Dtl_Nm",

IFNULL(CAST(T2."Street" AS NVARCHAR(250)),T4."Street" )  as "Disp_Dtl_Addr1",
case when t0."DocType"='S' then  IFNULL(CAST(T4."Building"  AS NVARCHAR(250)),'') else
IFNULL(CAST(T2."StreetNo" AS NVARCHAR(250)),T4."Block") ||',' || IFNULL(CAST(T2."Block" AS NVARCHAR(250)),T4."Building") end as "Disp_Dtl_Addr2",

IFNULL(T2."City",T4."City")  as "Disp_Dtl_Loc",
case when t0."DocType"='S' then T4."ZipCode" else
IFNULL(T2."ZipCode",T4."ZipCode") end "Disp_Dtl_Pin",
Case when t0."DocType"='S' then T5."GSTCode" else 
T11."GSTCode" end "Disp_Dtl_Stcd",


T9."GSTRegnNo" as "ShipDtls_GSTIN",T0."CardName" as "ShipDtls_LglNm"
,T0."CardName"  as "ShipDtls_TrdNm",
---- dispatch to -- Ship to address
T10."Street" "ShipDtls_Addr1", 
T10."Block" as "ShipDtls_Addr2",
T10."City" "ShipDtls_Loc",
--'Telangana' "ShipDtls_Loc",
T10."ZipCode" as "ShipDtls_Pin",
left(T10."GSTRegnNo",2)   as "ShipDtls_Stcd",

(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN4 I1 WHERE I1."BaseSum">=0 and I1."staType" IN (-110,-120) AND I1."DocEntry" = T0."DocEntry"  ) AS "AssVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN4 I1 WHERE I1."TaxSum">=0 and I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry"  ) AS "SgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN4 I1 WHERE I1."TaxSum">=0 and I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry"  ) AS "CgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN4 I1 WHERE I1."TaxSum">=0 and  I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" ) AS "IgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN4 I1 WHERE I1."TaxSum">=0 and  I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry"  ) AS "CesVal",
0.00 "StCesVal",
IFNULL(CAST(CAST(T0."RoundDif" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "RndOffAmt",

/*(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN4 I1 WHERE I1."staType" IN (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1')+
*/


IFNULL(CAST(CAST(T0."DocTotal" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "TotInvVal",
/*(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal", 5))+T0."TaxOnExp") AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RIN3 TT0 WHERE "DocEntry" = :DocEntry) AS "TCSVal"*/
0  "TCSVal"
,IFNULL((Select SUM(M0."GTotal"*-1) from RIN1 M0 where M0."DocEntry"=T0."DocEntry" and M0."Quantity"<0), '0.000')  as "DiscSum"  
,(SELECT "U_Url" FROM "@GIS_EI_OAPI" WHERE "U_Action"='GenerateEInvoice') as "URL",
(select  Replace(IFNULL(Max(M."Address2"),'ABC'),'"','')  from CRD1 M where M."CardCode"=T9."CardCode")"SAddress2",
(select  Replace(IFNULL(Max(M."Address3"),'ABC'),'"','')  from CRD1 M where M."CardCode"=T9."CardCode")"SAddress3"
--IFNULL(T9."Address3",'ABC') "SAddress3"
From ORIN T0
inner JOIN RIN1 T1 on T0."DocEntry" = T1."DocEntry" and T0."DocEntry" = :DocEntry
and  T0."CANCELED" ='N'
inner JOIN RIN12 T3 on T3."DocEntry" = T0."DocEntry"
inner join NNM1 F0 on F0."Series"=T0."Series"
Inner JOIN OCRD T8 ON T8."CardCode" =T0."CardCode"
LEFT OUTER JOIN OWHS T2 on T2."WhsCode" = T1."WhsCode"
LEFT OUTER JOIN OLCT T4 on T4."Code" = T1."LocCode" 
Left Outer join OCST T5 on T5."Code" = T4."State" and T5."Country" ='IN'
Left Outer join OCST T6 on T6."Code" = T3."StateB" and T6."Country" = T3."CountryB"
Left Outer Join OCST T7 on T7."Code" = T3."StateS" and T7."Country" = T3."CountryS"
Left Outer Join OCST T11 on T11."Code" = T2."State" and T11."Country" = 'IN'
LEFT OUTER JOIN :a T9 ON T9."CardCode" =T0."CardCode" AND T9."AdresType" ='B' and T9."Address"=T0."PayToCode"  
LEFT OUTER JOIN :a T10 ON T10."CardCode" =T0."CardCode" AND T10."AdresType" ='S' and T10."Address"=T0."ShipToCode"
WHERE T0."DocEntry"=:DocEntry and :DocType ='14'

union all

Select Distinct :ObjType AS "ObjType", '1.1' AS "Version",'GST' AS "TaxSch",'N' AS "RegRev",'N' AS "IgstOnIntra"
,:DocEntry as "DocEntry",'B2B' as "SupTyp" ,'DBN' as "Type",
"SeriesName"||'/'||cast(T0."DocNum" as varchar(20))  as "DocNum",TO_VARCHAR(CAST(T0."DocDate" AS varchar(10)),'DD/MM/YYYY') as "Dt" ,
T4."GSTRegnNo" as "Seller_GSTIN",IFNULL((select Top 1 IFNULL("PrintHdrF","CompnyName") from OADM),'JAY EXTRUTECH') as "Seller_Trade_Name",
--- seller

IFNULL(T4."Street" ,T2."Street" ) "Seller_Addr1",  
IFNULL(CAST(T4."Block" AS NVARCHAR(250)),T2."StreetNo") as "Seller_Addr2",
IFNULL(T4."City",T2."City") as "Seller_Loc",
IFNULL(T4."ZipCode",T2."City") as "Seller_Pin",
IFNULL(T5."GSTCode",'') as "Seller_State",
IFNULL(T2."Address2",'9000310598') as "Seller_Ph" ,
IFNULL((SELECT "E_Mail" FROM OADM),'info@jayextrutech.com') as "Seller_Em",

---- Seller

--- buyer add= bill to address
T9."GSTRegnNo" as "Buyer_GSTIN",
T0."CardName" as "Buyer_LglNm",
T0."CardName" as "Buyer_TrdNm",
IFNULL(T3."BPStatGSTN",T7."GSTCode") as "Buyer_Pos",
T9."Street" as "Buyer_Addr1",
T9."Block" "Buyer_Addr2",
T9."City"  as "Buyer_Loc",
T9."ZipCode" "Buyer_Pin",
left(T9."GSTRegnNo",2)  as "Buyer_State",
CASE WHEN IFNULL(T8."Cellular",'') = '' THEN '9000310598' ELSE LEFT(IFNULL(T8."Cellular",''),10) END as "Buyer_Ph",
CASE WHEN IFNULL(T8."E_Mail",'test1@gmail.com')='' THEN 'test1@gmail.com' ELSE 'test1@gmail.com' END as "Buyer_Em",

--- Dispatch from = Warehouse address
IFNULL((select Top 1 IFNULL("PrintHdrF","CompnyName") from OADM),'JAY EXTRUTECH') as "Disp_Dtl_Nm",

IFNULL(CAST(T2."Street" AS NVARCHAR(250)),T4."Street" )  as "Disp_Dtl_Addr1",
IFNULL(CAST(T2."StreetNo" AS NVARCHAR(250)),T4."Block") ||',' || IFNULL(CAST(T2."Block" AS NVARCHAR(250)),T4."Building") as "Disp_Dtl_Addr2",
IFNULL(T2."City",T4."City")  as "Disp_Dtl_Loc",
IFNULL(T2."ZipCode",T4."ZipCode") "Disp_Dtl_Pin",
CASE WHEN T0."DocType" = 'S' THEN T5."GSTCode" ELSE T11."GSTCode" END AS "Disp_Dtl_Stcd",

T9."GSTRegnNo" as "ShipDtls_GSTIN",T0."CardName" as "ShipDtls_LglNm"
,T0."CardName"  as "ShipDtls_TrdNm",
---- dispatch to -- Ship to address
T10."Street" "ShipDtls_Addr1", 
T10."Block" as "ShipDtls_Addr2",
T10."City" "ShipDtls_Loc",
--'Telangana' "ShipDtls_Loc",
T10."ZipCode" as "ShipDtls_Pin",
left(T10."GSTRegnNo",2)   as "ShipDtls_Stcd",

(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC4 I1 WHERE I1."BaseSum">=0 and I1."staType" IN (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1') AS "AssVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC4 I1 WHERE I1."TaxSum">=0 and I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1') AS "SgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC4 I1 WHERE I1."TaxSum">=0 and I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1') AS "CgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC4 I1 WHERE I1."TaxSum">=0 and  I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1') AS "IgstVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC4 I1 WHERE I1."TaxSum">=0 and  I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1') AS "CesVal",
0.00 "StCesVal",
IFNULL(CAST(CAST(T0."RoundDif" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "RndOffAmt",

/*(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 5))) AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC4 I1 WHERE I1."staType" IN (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."RelateType" = '1')+
*/


IFNULL(CAST(CAST(T0."DocTotal" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "TotInvVal",
/*(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal", 5))+T0."TaxOnExp") AS decimal(19, 2)) AS varchar(100)), '0.00')
FROM RPC3 TT0 WHERE "DocEntry" = :DocEntry) AS "TCSVal"*/
0 "TCSVal"
,IFNULL((Select SUM(M0."GTotal"*-1) from RPC1 M0 where M0."DocEntry"=T0."DocEntry" and M0."Quantity"<0), '0.000')  as "DiscSum"  
,(SELECT "U_Url" FROM "@GIS_EI_OAPI" WHERE "U_Action"='GenerateEInvoice') as "URL",
(select  Replace(IFNULL(Max(M."Address2"),'ABC'),'"','')  from CRD1 M where M."CardCode"=T9."CardCode")"SAddress2",
(select  Replace(IFNULL(Max(M."Address3"),'ABC'),'"','')  from CRD1 M where M."CardCode"=T9."CardCode")"SAddress3"
--IFNULL(T9."Address3",'ABC') "SAddress3"
From ORPC T0
inner JOIN RPC1 T1 on T0."DocEntry" = T1."DocEntry" and T0."DocEntry" = :DocEntry
and  T0."CANCELED" ='N'
inner JOIN RPC12 T3 on T3."DocEntry" = T0."DocEntry"
inner join NNM1 F0 on F0."Series"=T0."Series"
Inner JOIN OCRD T8 ON T8."CardCode" =T0."CardCode"
LEFT OUTER JOIN OWHS T2 on T2."WhsCode" = T1."WhsCode"
LEFT OUTER JOIN OLCT T4 on T4."Code" = T1."LocCode" 
Left Outer join OCST T5 on T5."Code" = T4."State" and T5."Country" ='IN'
Left Outer join OCST T6 on T6."Code" = T3."StateB" and T6."Country" = T3."CountryB"
Left Outer Join OCST T7 on T7."Code" = T3."StateS" and T7."Country" = T3."CountryS"
Left Outer Join OCST T11 on T11."Code" = T2."State" and T11."Country" = 'IN'
LEFT OUTER JOIN :a T9 ON T9."CardCode" =T0."CardCode" AND T9."AdresType" ='B' and T9."Address"=T0."PayToCode"  
LEFT OUTER JOIN :a T10 ON T10."CardCode" =T0."CardCode" AND T10."AdresType" ='S' and T10."Address"=T0."ShipToCode"
WHERE T0."DocEntry"=:DocEntry and :DocType ='19'


;

SELECT * FROM "#HdrDet";
DROP Table "#HdrDet";



ELSEIF :DType ='Detail' THEN
Create local temporary table "#LineDet" ("Line_Details" nvarchar(100),"DocEntry" nvarchar(100),"IsServc" nvarchar(10),
"LineNum" nvarchar(100),"PrdDesc" nvarchar(100),
"HsnCd" nvarchar(100),
"Unit" nvarchar(100),"Qty" numeric(18,2),	
"UnitPrice" numeric(18,2),"Discount" numeric(18,2),"TotAmt" numeric(18,2)
,"GstRt" numeric(18,2),"SgstAmt" numeric(18,2), "CgstAmt"	numeric(18,2), 
"IgstAmt" numeric(18,2),"CesAmt" numeric(18,2),"CesNonAdvlAmt" numeric(18,2),"CesRt" numeric(18,2),
"StateCesRt" numeric(18,2),"StateCesAmt" numeric(18,2),"StateCesNonAdvlAmt" numeric(18,2)
,"OthChrg" numeric(18,2),"TotItemVal" numeric(18,2),"AssAmt" numeric(18,2),"TCSVal" numeric(18,2));

-- Line Deatils 
INSERT INTO "#LineDet" 
(

Select 'Line_Details' as "Line_Details" 
,Cast(ifnull(:Docentry,'-1') as int) as "DocEntry"
, CASE WHEN T0."DocType" ='I' AND T6."InvntItem"='Y'  
THEN 'N' ELSE 'Y' END AS "IsServc"
,IFNULL(CAST(CAST(T1."LineNum" AS integer) AS varchar(100)), '') AS "LineNum"
, IFNULL(T1."Dscription",T2."AcctName") AS "PrdDesc",
REPLACE(CASE WHEN T0."DocType" ='I' and T6."InvntItem"='N' THEN left(REPLACE(G."ChapterID",'.',''),6) 
 WHEN T0."DocType" ='I' and T6."InvntItem"='Y'  then left(REPLACE(G."ChapterID",'.',''),8)  
ELSE REPLACE(H."ServCode",'0','') END,'.', '') AS "HsnCd"
,'NOS' as "Unit",

IFNULL(CAST(CASE WHEN T0."DocType" ='I' THEN CAST(T1."Quantity" AS decimal(19, 2)) ELSE 1 END AS varchar(100)), '0.000') AS "Qty",
IFNULL(CAST(CASE 
  WHEN T0."DocType" ='I' AND T1."TaxOnly" = 'Y' THEN CAST(T1."Price" AS decimal(19, 2))
  WHEN T0."DocType" ='I' THEN CAST(IFNULL(T1."INMPrice",
    (SELECT IFNULL("BaseSum",0) FROM INV4 A WHERE A."DocEntry"=T0."DocEntry" 
    and A."LineNum"=T1."LineNum" AND A."staType" in (-110,-120) AND A."RelateType" = '1')) AS decimal(19, 2))
  ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "UnitPrice",
0 AS "Discount",
--IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST(T1."Quantity"*T1."INMPrice" AS decimal(19, 2)) 
--ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum",2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "TotAmt", 
IFNULL(CAST(CAST(T1."VatPrcnt" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "GstRt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "SgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "CgstAmt", 
(SELECT IFNULL(CAST(CAST((ROUND(SUM(I1."TaxSum"),2)) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "IgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "CesAmt", 
0.00 "CesNonAdvlAmt",
0.00 AS "CesRt",
0.00 AS "StateCesRt",0.00 AS "StateCesAmt",0.00 AS "StateCesNonAdvlAmt", 0.00 AS "OthChrg",
IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST((T1."Quantity"*T1."INMPrice")+ROUND(T1."VatSum",2) AS decimal(19, 2)) ELSE CAST(T1."LineTotal"+T1."VatSum" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotItemVal", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "AssAmt", '0' as "TCSVal"
--(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal" +T0."TaxOnExp", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
--FROM INV3 TT0 WHERE TT0."DocEntry" =T0."DocEntry"  AND T1."LineNum" =0) AS "TCSVal"

From OINV T0 
INNER JOIN INV1 T1 on T0."DocEntry" = T1."DocEntry"
and  T0."CANCELED" ='N'
and (Case When T0."DocType"='S' then 1 else T1."Quantity" end)>=1
INNER JOIN OACT T2 on T2."AcctCode"=T1."AcctCode"
LEFT OUTER JOIN OITM T6 on T6."ItemCode" = T1."ItemCode" 
LEFT OUTER JOIN OCHP G ON G."AbsEntry"=T6."ChapterID"
LEFT OUTER JOIN OSAC H ON H."AbsEntry"=T1."SacEntry"

WHERE T0."DocEntry" = :DocEntry and :DocType='13'

union all

Select 'Line_Details' as "Line_Details" 
,T0."DocEntry" as "DocEntry"
, 'N'  "IsServc"
,(Select IFNULL(CAST(CAST(Max(M0."LineNum")+1 AS integer) AS varchar(100)), '') from INV1 M0 where M0."DocEntry"=T0."DocEntry") AS "LineNum"
, 'Freight Charges' AS "PrdDesc",
(Select left(REPLACE(G."ChapterID",'.',''),8) from
INV1 T11 inner JOIN OITM T6 on T6."ItemCode" = T11."ItemCode" and t11."VisOrder"=0
inner JOIN OCHP G ON G."AbsEntry"=T6."ChapterID"
where t11."DocEntry"= T1."DocEntry" ) AS "HsnCd"
,'NOS' as "Unit",
'1' AS "Qty",
(Select Max("LineTotal") from INV3 M0 where M0."DocEntry" = T0."DocEntry") AS "UnitPrice",
0 AS "Discount",
--IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST(T1."Quantity"*T1."INMPrice" AS decimal(19, 2)) 
--ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum",2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "TotAmt", 
IFNULL(CAST(CAST(T1."VatPrcnt" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "GstRt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "SgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "CgstAmt", 
(SELECT IFNULL(CAST(CAST((ROUND(SUM(I1."TaxSum"),2)) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "IgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "CesAmt", 
0.00 "CesNonAdvlAmt",
0.00 AS "CesRt",
0.00 AS "StateCesRt",0.00 AS "StateCesAmt",0.00 AS "StateCesNonAdvlAmt", 0.00 AS "OthChrg",
(Select IFNULL(CAST(CAST((M0."LineTotal"+ROUND(M0."VatSum",2)) AS decimal(19, 2)) AS varchar(100)), '0.000')
  from INV3 M0 where M0."DocEntry"=T0."DocEntry" ) AS "TotItemVal",
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM INV4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "AssAmt", '0' as "TCSVal"
--(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal" +T0."TaxOnExp", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
--FROM INV3 TT0 WHERE TT0."DocEntry" =T0."DocEntry"  AND T1."LineNum" =0) AS "TCSVal"

From OINV T0 
INNER JOIN INV1 T1 on T0."DocEntry" = T1."DocEntry" and T1."VisOrder"=0 and T0."TotalExpns">0
and (Case When T0."DocType"='S' then 1 else T1."Quantity" end)>=1
and  T0."CANCELED" ='N'
INNER JOIN OACT T2 on T2."AcctCode"=T1."AcctCode"
LEFT OUTER JOIN OITM T6 on T6."ItemCode" = T1."ItemCode" 
LEFT OUTER JOIN OCHP G ON G."AbsEntry"=T6."ChapterID"
LEFT OUTER JOIN OSAC H ON H."AbsEntry"=T1."SacEntry"

WHERE T0."DocEntry" = :DocEntry and :DocType='13'


union all

Select 'Line_Details' as "Line_Details" 
,Cast(ifnull(:Docentry,'-1') as int) as "DocEntry"
, CASE WHEN T0."DocType" ='I' AND T6."InvntItem"='Y'  THEN 'N' ELSE 'Y' END AS "IsServc"
,IFNULL(CAST(CAST(T1."LineNum" AS integer) AS varchar(100)), '') AS "LineNum"
, IFNULL(T1."Dscription",T2."AcctName") AS "PrdDesc",
REPLACE(CASE WHEN T0."DocType" ='I' and T6."InvntItem"='N' THEN left(REPLACE(G."ChapterID",'.',''),6) 
 WHEN T0."DocType" ='I' and T6."InvntItem"='Y'  then left(REPLACE(G."ChapterID",'.',''),8)  
ELSE REPLACE(H."ServCode",'00','') END,'.', '') AS "HsnCd"
,'NOS' as "Unit",

IFNULL(CAST(CASE WHEN T0."DocType" ='I' THEN CAST(T1."Quantity" AS decimal(19, 2)) ELSE 1 END AS varchar(100)), '0.000') AS "Qty",
IFNULL(CAST(CASE WHEN T0."DocType" ='I' THEN CAST(IFNULL(T1."INMPrice",
(SELECT IFNULL("BaseSum",0) FROM RIN4 A WHERE A."DocEntry"=T0."DocEntry" 
and A."LineNum"=T1."LineNum" AND A."staType" in (-110,-120) AND A."RelateType" = '1')) AS decimal(19, 2))
 ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "UnitPrice",
0 AS "Discount",
--IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST(T1."Quantity"*T1."INMPrice" AS decimal(19, 2)) 
--ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum",2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "TotAmt", 
IFNULL(CAST(CAST(T1."VatPrcnt" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "GstRt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "SgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "CgstAmt", 
(SELECT IFNULL(CAST(CAST((ROUND(SUM(I1."TaxSum"),2)) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "IgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "CesAmt", 
0.00 "CesNonAdvlAmt",
0.00 AS "CesRt",
0.00 AS "StateCesRt",0.00 AS "StateCesAmt",0.00 AS "StateCesNonAdvlAmt", 0.00 AS "OthChrg",
IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST((T1."Quantity"*T1."INMPrice")+ROUND(T1."VatSum",2) AS decimal(19, 2)) ELSE CAST(T1."LineTotal"+T1."VatSum" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotItemVal", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "AssAmt", '0' as "TCSVal"
--(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal" +T0."TaxOnExp", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
--FROM RIN3 TT0 WHERE TT0."DocEntry" =T0."DocEntry"  AND T1."LineNum" =0) AS "TCSVal"

From ORIN T0 
INNER JOIN RIN1 T1 on T0."DocEntry" = T1."DocEntry"
and (Case When T0."DocType"='S' then 1 else T1."Quantity" end)>=1
and  T0."CANCELED" ='N'
INNER JOIN OACT T2 on T2."AcctCode"=T1."AcctCode"
LEFT OUTER JOIN OITM T6 on T6."ItemCode" = T1."ItemCode" 
LEFT OUTER JOIN OCHP G ON G."AbsEntry"=T6."ChapterID"
LEFT OUTER JOIN OSAC H ON H."AbsEntry"=T1."SacEntry"

WHERE T0."DocEntry" = :DocEntry and :DocType='14'


union all

Select 'Line_Details' as "Line_Details" 
,Cast(ifnull(:Docentry,'-1') as int) as "DocEntry"
, CASE WHEN T0."DocType" ='I' AND T6."InvntItem"='Y'  THEN 'N' ELSE 'Y' END AS "IsServc"
,(Select IFNULL(CAST(CAST(Max(M0."LineNum")+1 AS integer) AS varchar(100)), '') from RIN1 M0 where M0."DocEntry"=T0."DocEntry") AS "LineNum"
, 'Freight Charges' AS "PrdDesc",
REPLACE(CASE WHEN T0."DocType" ='I' and T6."InvntItem"='N' THEN left(REPLACE(G."ChapterID",'.',''),6) 
 WHEN T0."DocType" ='I' and T6."InvntItem"='Y'  then left(REPLACE(G."ChapterID",'.',''),8)  
ELSE REPLACE(H."ServCode",'00','') END,'.', '') AS "HsnCd"
,'NOS' as "Unit",

'1' AS "Qty",
(Select Max("LineTotal") from RIN3 M0 where M0."DocEntry" = T0."DocEntry")  AS "UnitPrice",
0 AS "Discount",
--IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST(T1."Quantity"*T1."INMPrice" AS decimal(19, 2)) 
--ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum",2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" 
AND I1."RelateType" = '3') AS "TotAmt", 
IFNULL(CAST(CAST(T1."VatPrcnt" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "GstRt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry"
AND I1."RelateType" = '3') AS "SgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry"
AND I1."RelateType" = '3') AS "CgstAmt", 
(SELECT IFNULL(CAST(CAST((ROUND(SUM(I1."TaxSum"),2)) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry"
AND I1."RelateType" = '3') AS "IgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '3') AS "CesAmt", 
0.00 "CesNonAdvlAmt",
0.00 AS "CesRt",
0.00 AS "StateCesRt",0.00 AS "StateCesAmt",0.00 AS "StateCesNonAdvlAmt", 0.00 AS "OthChrg",
(Select IFNULL(CAST(CAST((M0."LineTotal"+ROUND(M0."VatSum",2)) AS decimal(19, 2)) AS varchar(100)), '0.000')
  from RIN3 M0 where M0."DocEntry"=T0."DocEntry" ), 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RIN4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" 
AND I1."RelateType" = '3') AS "AssAmt", '0' as "TCSVal"
--(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal" +T0."TaxOnExp", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
--FROM RIN3 TT0 WHERE TT0."DocEntry" =T0."DocEntry"  AND T1."LineNum" =0) AS "TCSVal"

From ORIN T0 
INNER JOIN RIN1 T1 on T0."DocEntry" = T1."DocEntry" and T1."VisOrder"=0 and T0."TotalExpns">0
and (Case When T0."DocType"='S' then 1 else T1."Quantity" end)>=1
and  T0."CANCELED" ='N'
INNER JOIN OACT T2 on T2."AcctCode"=T1."AcctCode"
LEFT OUTER JOIN OITM T6 on T6."ItemCode" = T1."ItemCode" 
LEFT OUTER JOIN OCHP G ON G."AbsEntry"=T6."ChapterID"
LEFT OUTER JOIN OSAC H ON H."AbsEntry"=T1."SacEntry"

WHERE T0."DocEntry" = :DocEntry and :DocType='14'



union all

Select 'Line_Details' as "Line_Details" 
,Cast(ifnull(:Docentry,'-1') as int) as "DocEntry"
, CASE WHEN T0."DocType" ='I' AND T6."InvntItem"='Y'  
THEN 'N' ELSE 'Y' END AS "IsServc"
,IFNULL(CAST(CAST(T1."LineNum" AS integer) AS varchar(100)), '') AS "LineNum"
, IFNULL(T1."Dscription",T2."AcctName") AS "PrdDesc",
REPLACE(CASE WHEN T0."DocType" ='I' and T6."InvntItem"='N' THEN left(REPLACE(G."ChapterID",'.',''),6) 
 WHEN T0."DocType" ='I' and T6."InvntItem"='Y'  then left(REPLACE(G."ChapterID",'.',''),8)  
ELSE REPLACE(H."ServCode",'0','') END,'.', '') AS "HsnCd"
,'NOS' as "Unit",

IFNULL(CAST(CASE WHEN T0."DocType" ='I' THEN CAST(T1."Quantity" AS decimal(19, 2)) ELSE 1 END AS varchar(100)), '0.000') AS "Qty",
IFNULL(CAST(CASE WHEN T0."DocType" ='I' THEN CAST(IFNULL(T1."INMPrice",
(SELECT IFNULL("BaseSum",0) FROM RPC4 A WHERE A."DocEntry"=T0."DocEntry" 
and A."LineNum"=T1."LineNum" AND A."staType" in (-110,-120) AND A."RelateType" = '1')) AS decimal(19, 2))
 ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "UnitPrice",
0 AS "Discount",
--IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST(T1."Quantity"*T1."INMPrice" AS decimal(19, 2)) 
--ELSE CAST(T1."LineTotal" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum",2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RPC4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "TotAmt", 
IFNULL(CAST(CAST(T1."VatPrcnt" AS decimal(19, 2)) AS varchar(100)), '0.000') AS "GstRt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RPC4 I1 WHERE I1."staType" = -110 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "SgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RPC4 I1 WHERE I1."staType" = -100 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "CgstAmt", 
(SELECT IFNULL(CAST(CAST((ROUND(SUM(I1."TaxSum"),2)) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RPC4 I1 WHERE I1."staType" = -120 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "IgstAmt", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RPC4 I1 WHERE I1."staType" = -130 AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "CesAmt", 
0.00 "CesNonAdvlAmt",
0.00 AS "CesRt",
0.00 AS "StateCesRt",0.00 AS "StateCesAmt",0.00 AS "StateCesNonAdvlAmt", 0.00 AS "OthChrg",
IFNULL(CAST(CASE WHEN T0."DocType"='I' THEN CAST((T1."Quantity"*T1."INMPrice")+ROUND(T1."VatSum",2) AS decimal(19, 2)) ELSE CAST(T1."LineTotal"+T1."VatSum" AS decimal(19, 2)) END AS varchar(100)), '0.000') AS "TotItemVal", 
(SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."BaseSum", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
FROM RPC4 I1 WHERE I1."staType" in (-110,-120) AND I1."DocEntry" = T0."DocEntry" AND I1."LineNum" = T1."LineNum" 
AND I1."RelateType" = '1') AS "AssAmt", '0' as "TCSVal"
--(SELECT IFNULL(CAST(CAST((SUM(ROUND(TT0."LineTotal" +T0."TaxOnExp", 2))) AS decimal(19, 2)) AS varchar(100)), '0.000') 
--FROM RPC3 TT0 WHERE TT0."DocEntry" =T0."DocEntry"  AND T1."LineNum" =0) AS "TCSVal"

From ORPC T0 
INNER JOIN RPC1 T1 on T0."DocEntry" = T1."DocEntry"
and (Case When T0."DocType"='S' then 1 else T1."Quantity" end)>=1
and  T0."CANCELED" ='N'
INNER JOIN OACT T2 on T2."AcctCode"=T1."AcctCode"
LEFT OUTER JOIN OITM T6 on T6."ItemCode" = T1."ItemCode" 
LEFT OUTER JOIN OCHP G ON G."AbsEntry"=T6."ChapterID"
LEFT OUTER JOIN OSAC H ON H."AbsEntry"=T1."SacEntry"

WHERE T0."DocEntry" = :DocEntry and :DocType='19'

);

SELECT * FROM "#LineDet";
DROP Table "#LineDet";
END IF;
END;