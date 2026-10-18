-- ============================================================================
-- 04: GIS_EwayBill_GetPostDet (JEGIS - A/R Invoice E-Way Bill Generation)
-- Execute this script in the target SAP HANA company schema (JAY_EXTRUTECH)
-- ============================================================================

CREATE OR REPLACE PROCEDURE "GIS_EwayBill_GetPostDet"(
IN DocEntry varchar(50),IN Action varchar(50)
,IN Aspid varchar(50),IN Asppassword varchar(50)
,IN Gstin varchar(50),IN Username varchar(50)
,IN Authtoken varchar(100),IN VehicleNo varchar(100)
,IN Transgst nvarchar(100),IN Transname nvarchar(100)
,IN DocType nvarchar(20),IN ReqType NVARCHAR(10))
AS  
BEGIN

If :DocType='Invoice' AND :ReqType='Detail' THEN

   SELECT A."DocNum" AS "docNo", B."ItemCode" AS "itemNo", F."ItmsGrpNam" AS "productName"
   , IFNULL(E."ItemCode", '') AS "productDesc"
   , left(COALESCE(REPLACE(G."ChapterID", '.', ''), ' '),8) AS "hsnCode"
   , CAST(CAST(B."Quantity" AS decimal(19, 2)) AS varchar(100)) AS "quantity"
   , CASE WHEN B."unitMsr" = 'KG' THEN 'KILOGRAMS' WHEN B."unitMsr" = 'MT' THEN 'METRIC TON' 
   WHEN B."unitMsr" = 'LTR' THEN 'LITRES' WHEN B."unitMsr" = 'NOS' 
   THEN 'NOS' WHEN B."unitMsr" = 'PCS' THEN 'PIECES' ELSE 'NOS' END AS "qtyUnit"
   , CAST(CAST(B."Quantity" * B."Price" AS decimal(19, 3)) AS varchar(100)) AS "taxableAmount",
   (SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxRate", 3))) AS decimal(19, 3)) AS varchar(100)), '0.000') 
   FROM INV4 I1 WHERE I1."staType" = -110 AND I1."DocEntry" = A."DocEntry" 
   AND I1."LineNum" = b."LineNum" AND I1."RelateType"='1') AS "sgstRate"
   , (SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxRate", 3))) AS decimal(19, 3)) AS varchar(100)), '0.000')
   FROM INV4 I1 WHERE I1."staType" = -100 AND I1."DocEntry" = A."DocEntry" AND i1."LineNum" = b."LineNum" AND I1."RelateType"='1') AS "cgstRate", 
   (SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxRate", 3))) AS decimal(19, 3)) AS varchar(100)), '0.000') FROM INV4 I1 
   WHERE I1."staType" = -120 AND I1."DocEntry" = A."DocEntry" AND I1."LineNum" = b."LineNum" AND I1."RelateType"='1') AS "igstRate", 
   (SELECT IFNULL(CAST(CAST((SUM(ROUND(I1."TaxRate", 3))) AS decimal(19, 3)) AS varchar(100)), '0.000') FROM INV4 I1 
   WHERE I1."staType" = -130 AND I1."DocEntry" = A."DocEntry" AND I1."LineNum" = b."LineNum" AND I1."RelateType"='1') AS "cessRate", 
   '0.000' AS "cessNonAdvol" FROM OINV A 
   INNER JOIN INV1 B ON A."DocEntry" = B."DocEntry" 
    INNER JOIN INV12 C ON C."DocEntry" = A."DocEntry"
    INNER JOIN OLCT D ON D."Code" = B."LocCode" 
    INNER JOIN OWHS H ON H."WhsCode" = B."WhsCode" 
    INNER JOIN OITM E ON E."ItemCode" = B."ItemCode" 
    INNER JOIN OITB F ON F."ItmsGrpCod" = E."ItmsGrpCod" 
    INNER JOIN OCHP G ON G."AbsEntry" = E."ChapterID" WHERE A."DocEntry" = :DocEntry;
 
ELSEIf :DocType='Invoice' AND :ReqType='Header' THEN

 b=Select A1."DocEntry", D."ZipCode" "Loc_Pin", H."ZipCode" "UDT_Pin",
 H."Street" "UDT_Add1", H."Block" "UDT_Add2", H."City" "UDT_Loc",
 IFNULL(A4."GSTCode", '') "Loc_State",
 IFNULL(A4."GSTCode", '') "UDT_State"
  ,
  case when A1."UseBilAddr"='Y' then A5."StreetS" else IFNULL(CAST(IFNULL(A5."StreetB", A5."StreetS") AS varchar(100)), '') end AS "toAddr1"
 ,case when A1."UseBilAddr"='Y' then A5."BlockS" else IFNULL(CAST(IFNULL(A5."BlockB", A5."BlockS") AS varchar(100)), '') end AS "toAddr2"
 ,D."City" AS "toPlace"
,A5."ZipCodeB" AS "toPincode_Bill"
,A5."ZipCodeS" AS "toPincode_Ship",A7."GSTCode" "actToStateCode_Bill",A6."GSTCode" "actToStateCode_Ship" 
,D."Location" , D."Location" "Name",
 case when A1."UseBilAddr"='Y' then A5."BpGSTN" else N4."GSTRegnNo" end "BillToGST",N5."GSTRegnNo" "ShipToGST",
A1."U_TransType" "TransType"
  from OINV A1  
  INNER JOIN INV1 A2 ON A2."DocEntry" = A1."DocEntry"
  INNER JOIN INV12 C ON C."DocEntry" = A1."DocEntry"
  INNER JOIN OLCT D ON D."Code" = A2."LocCode"
  INNER JOIN OWHS H ON H."WhsCode" = A2."WhsCode"
  INNER JOIN OCST A4 ON A4."Code" = D."State" AND A4."Country" = D."Country" 
  LEFT OUTER JOIN INV12 A5 ON A5."DocEntry" = A1."DocEntry" 
  LEFT OUTER JOIN OCST A6 ON A6."Code" = A5."StateS" AND A6."Country" = A5."CountryS" 
  LEFT OUTER JOIN OCST A7 ON A7."Code" = A5."StateB" AND A7."Country" = A5."CountryB" 
  Left Join OCRD N1 on N1."CardCode"=A1."U_VendCode"
  Left join CRD1 N2 on N2."CardCode"=N1."CardCode" and N2."AdresType"='S'
  Left Join CRD1 N4 on N4."CardCode"=A1."CardCode" and N4."Address"=A1."ShipToCode" and N4."AdresType"='S'
   Left Join CRD1 N5 on N5."CardCode"=A1."CardCode" and N5."Address"=A1."PayToCode" and N5."AdresType"='B'
  WHERE A1."DocEntry"=:DocEntry limit 1;

SELECT DISTINCT 'O' AS "supplyType",'1' AS "subSupplyType" ,'' AS "subSupplyDesc"
,CASE WHEN A1."GSTTranTyp" = 'GA' THEN 'INV' WHEN A1."GSTTranTyp" like '%-%' THEN 'Bill of Supply' ELSE 'Others' END AS "docType"
,F0."SeriesName"||'/'||CAST(A1."DocNum" AS nvarchar(50)) AS "docNo"

, TO_VARCHAR (TO_DATE(A1."DocDate"), 'DD/MM/YYYY') AS "docDate"
, IFNULL(D."GSTRegnNo", '') AS "fromGstin"
,IFNULL((SELECT TOP 1 IFNULL("PrintHeadr", "CompnyName") FROM OADM), 'JAY EXTRUTECH') AS "fromTrdName"
,Case when S0."TransType" in ('1','2') then H."Street" else IFNULL(S0."UDT_Add1",H."Street") end AS "fromAddr1"
,Case when S0."TransType" in ('1','2') then IFNULL(H."Block",'1') else IFNULL(S0."UDT_Add2",H."Block") end AS "fromAddr2"
,Case when S0."TransType" in ('1','2') then H."City" else IFNULL(S0."UDT_Loc",H."City") end "fromPlace"
,Case when S0."TransType" in ('1','2') then S0."Loc_Pin" Else S0."UDT_Pin" end AS "fromPincode",
Case when S0."TransType" in ('1','2') then S0."Loc_State" Else S0."UDT_State" end as "actFromStateCode"

, S0."Loc_State"   AS "fromStateCode",

 Upper(S0."BillToGST")  AS"toGstin"

,A1."CardName" AS "toTrdName"
,S0."toAddr1"
 ,S0."toAddr2"
 ,S0."toPlace"
, CASE WHEN S0."TransType" IN ('2','4') THEN S0."toPincode_Ship" ELSE S0."toPincode_Bill" END AS "toPincode"

,CASE 
  WHEN S0."TransType" IN ('2','4') 
       THEN S0."actToStateCode_Ship"
  ELSE S0."actToStateCode_Bill"
END AS "actToStateCode"

 , S0."actToStateCode_Bill"
 AS "toStateCode"

 ,S0."TransType" AS "transactionType"
 ,'' AS "dispatchFromGSTIN"
 ,Case when S0."TransType" in ('1','2') then S0."Location" When S0."TransType" in ('3','4') then S0."Name" end  AS "dispatchFromTradeName"
 ,Case when S0."TransType" in ('1','3') then  S0."BillToGST"  else S0."ShipToGST" end AS "shipToGSTIN"
 ,A1."CardName" AS "shipToTradeName"
 ,-round(A1."DiscSum",0) AS "otherValue"
 ,CAST(ROUND((SELECT IFNULL(CAST((SUM(ROUND(I1."LineTotal", 3))) AS varchar(100)), '0')
    FROM INV1 I1 WHERE I1."DocEntry" = A1."DocEntry"), 3) + CAST(ROUND(A1."TotalExpns", 3) AS varchar(100)) AS varchar(100)) AS "totalValue"
    
,CASE 
  WHEN 
    ( S0."Loc_State")
    <>
    ( S0."actToStateCode_Bill")
  THEN CAST(CAST(IFNULL(IGST2."IGST_Sum", 0) AS DECIMAL(19,3)) AS NVARCHAR(100))
  ELSE '0.000'
END AS "igstValue",

CASE 
  WHEN 
    ( S0."Loc_State")
    =
    ( S0."actToStateCode_Bill")
  THEN CAST(CAST(IFNULL(CGST2."CGST_Sum", 0) AS DECIMAL(19,3)) AS NVARCHAR(100))
  ELSE '0.000'
END AS "cgstValue",

CASE 
  WHEN 
    ( S0."Loc_State")
    =
    ( S0."actToStateCode_Bill")
  THEN CAST(CAST(IFNULL(SGST2."SGST_Sum", 0) AS DECIMAL(19,3)) AS NVARCHAR(100))
  ELSE '0.000'
END AS "sgstValue"

 ,CAST(CAST(IFNULL(CessGST."CessGST_Sum", 0) AS decimal(19, 3)) AS varchar(100)) AS "cessValue"
 ,0 AS "cessNonAdvolValue",CAST(A1."DocTotal" + IFNULL((SELECT SUM(M1."LineTotal") FROM INV1 M1 WHERE M1."DocEntry" = A1."DocEntry" AND M1."TaxOnly" = 'Y'), 0) AS decimal(19, 2)) AS "totInvValue"
,CASE WHEN :Transgst = '' THEN  IFNULL(N2."GSTRegnNo", '') ELSE :Transgst END AS "transporterId"
 ,IFNULL(N1."CardName", '') AS "transporterName"
 ,'0' AS "transDocNo"
 ,'0' AS "transDistance"
  , TO_VARCHAR (TO_DATE(A1."DocDate"), 'DD/MM/YYYY') AS "transDocDate"
 ,'1' AS "transMode"
,CASE WHEN :VehicleNo = '' THEN  A1."U_VehicleNo" ELSE :VehicleNo END AS "vehicleNo"
 ,'R' AS "vehicleType" 
 
  from OINV A1  
  INNER JOIN INV1 A2 ON A2."DocEntry" = A1."DocEntry"
  Inner join NNM1 F0 on F0."Series"=A1."Series"
  INNER JOIN INV12 C ON C."DocEntry" = A1."DocEntry"
  INNER JOIN OLCT D ON D."Code" = A2."LocCode"
  INNER JOIN OWHS H ON H."WhsCode" = A2."WhsCode"
  inner join :b S0 on S0."DocEntry"=A1."DocEntry"
  LEFT OUTER JOIN (SELECT IFNULL(SUM("TaxSum"), 0) AS "CGST_Sum", "DocEntry" FROM INV4 WHERE "staType" = '-100'
      GROUP BY "DocEntry") AS CGST2 ON CGST2."DocEntry" = A1."DocEntry" LEFT OUTER JOIN (SELECT IFNULL(SUM("TaxSum"), 0) AS "SGST_Sum", "DocEntry" FROM INV4 WHERE "staType" = '-110' GROUP BY "DocEntry") AS SGST2 
      ON SGST2."DocEntry" = A1."DocEntry" LEFT OUTER JOIN (SELECT IFNULL(SUM("TaxSum"), 0) AS "IGST_Sum", "DocEntry" FROM INV4 WHERE "staType" = '-120' GROUP BY "DocEntry") AS IGST2 
      ON IGST2."DocEntry" = A1."DocEntry"  
     LEFT OUTER JOIN (SELECT IFNULL(SUM("TaxSum"), 0) AS "CessGST_Sum", "DocEntry" FROM INV4 WHERE "staType" = '-130' GROUP BY "DocEntry") AS CessGST ON CessGST."DocEntry" = A1."DocEntry" INNER JOIN OCST A4 ON A4."Code" = D."State" 
      AND A4."Country" = D."Country" 
  LEFT OUTER JOIN INV12 A5 ON A5."DocEntry" = A1."DocEntry" 
  LEFT OUTER JOIN OCST A6 ON A6."Code" = A5."StateS" AND A6."Country" = A5."CountryS" 
  LEFT OUTER JOIN OCST A7 ON A7."Code" = A5."StateB" AND A7."Country" = A5."CountryB" 
  Left Join OCRD N1 on N1."CardCode"=A1."U_VendCode"
  Left join CRD1 N2 on N2."CardCode"=N1."CardCode" and N2."AdresType"='S'
  Left Join CRD1 N4 on N4."CardCode"=A1."CardCode" and N4."Address"=A1."ShipToCode" and N4."AdresType"='S'
   Left Join CRD1 N5 on N5."CardCode"=A1."CardCode" and N5."Address"=A1."PayToCode" and N5."AdresType"='B'
  WHERE A1."DocEntry"=:DocEntry;

END IF;

END;
