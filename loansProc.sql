USE [CBZCredit]
GO
/****** Object:  StoredProcedure [dbo].[allLOansToSendToNdasendaUSDLOANS]    Script Date: 9/6/2022 2:30:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


ALTER PROCEDURE  [dbo].[allLOansToSendToNdasendaUSDLOANS] 
   
AS select isPosted, ID,IDNO,ECNO,type,CONVERT(NVARCHAR(50),Reference) as Reference,ssbtopup,fcbStatusx,StartDate,EndDate,FORENAMES,SURNAME,replace(PaymentX,'.','') Payment,replace(totalx,'.','') total, len (ECNO) as length from (SELECT REPLACE(REPLACE(IDNO,'-',''),' ','') IDNO,isPosted,ID, ECNO,type,ID as [Reference],ssbtopup,fcbStatus as [fcbStatusx], CONVERT(nvarchar(8), FIN_REPAY_DATE, 112) as [StartDate], convert(nvarchar(8),(select MAX(PAYMENT_DATE) from AMORTIZATION_SCHEDULE  where LOANID = QA.ID  and Currency = 'USD'),112) as [EndDate], QA.FORENAMES , qa.SURNAME ,isnull((Select  Payment from AMORTIZATION_SCHEDULE where loanid= qa.id and PAYMENT_NO=1 and Currency = 'USD'),0) PaymentX, isnull((select sum(payment) from AMORTIZATION_SCHEDULE where LOANID=qa.id and Currency = 'USD' ),0) as totalx  FROM QUEST_APPLICATION QA WHERE STATUS in ('MCC APPROVAL','Loan Application Capture') and Currency = 'USD' and send_to in('4042','1024')and ECNO IS NOT NULL and ECNO<>'' and (isPosted is null or isPosted='NULL') and (ssbtopup is null or ssbtopup='0')   and LO_ID<>'164' and CUSTOMER_TYPE='Salary Based Personal' AND SUB_INDIVIDUAL='SSB' ) tt where PaymentX<>'' and totalx<> '' and startdate>= getdate() and EndDate> getdate() and len(ECNO)>7 and len(ECNO)<9 and IDNO<>'' and ID is not null and FORENAMES is not null and SURNAME is not null and fcbStatusx in ('GREEN','GOOD','FAIR')
		UNION ALL
	   SELECT nc.isposted,qa.id, REPLACE(qa.IDNO,'-','') IDNO,qa.ECNO,nc.type,qa.Reference as [Reference]  ,qa.ssbtopup,qa.fcbstatus as [fcbStatusx], CONVERT(nvarchar(8), nc.FirstRepaymentDate, 112) as [StartDate], CONVERT(nvarchar(8), nc.endDate, 112)  as [EndDate],QA.FORENAMES,QA.SURNAME,REPLACE(isnull((Select  Payment from AMORTIZATION_SCHEDULE where loanid= qa.id and PAYMENT_NO=1 and Currency = 'USD'),0),'.','') PaymentX, REPLACE(isnull((select sum(payment) from AMORTIZATION_SCHEDULE where LOANID=qa.id and Currency = 'USD'),0),'.','') totalx, len (ECNO) as length FROM NdasendaChanges nc left join QUEST_APPLICATION qa on qa.id=nc.loanid where nc.authorised is null and (nc.isposted is null or nc.isPosted='NULL')  and Currency = 'USD' and ECNO <> '' and NC.FirstRepaymentDate>= getdate() and (qa.ssbtopup is null or qa.ssbtopup='0')

 
	 
	

