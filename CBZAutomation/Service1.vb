Imports System.Configuration
Imports System.Data.SqlClient
Imports System.IO
Imports System.Net.Configuration
Imports System.Net.Mail
Imports System.Net
Imports System.Text
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Diagnostics
Imports System.Linq
Imports System.ServiceProcess
Imports System.Threading.Tasks

Imports System.Timers
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.Web.Script.Serialization


Public Class Service1
    Dim Scope As String, Token_Type As String, Access_Token As String, Expires_In As Integer, Refresh_Token As String, TokenExpiryDate As DateTime
    Dim id As String, idnumber As String, ecnumber As String, reference As String, name As String, transdate As String, amount As Double, startdate As String, enddate As String, branch As String, bankAccount As String, totalAmount As String, status As String, message As String, recordsCount As String, deductionCode As String, creationDate As Date, records As List(Of Object), surname As String, securityToken As String, type As String, batchID As String
    Dim cnstr As String = ConfigurationManager.ConnectionStrings("Constring2").ConnectionString
    Dim FILE_NAME As String = "C:\inetpub\wwwroot\ErrorLogFile.txt"
    Dim adp As SqlDataAdapter
    Dim cmd As SqlCommand
    Dim con As New SqlConnection
    Dim connection As String
    Dim ds As New DataSet()
    Dim fileName As String = "C:\Logs\NdasendaLog.txt"
    Dim Posturl As String, username As String, password As String
    Public Property ErrorLogging As Object
    Private tt As Timer = Nothing
    Protected Overrides Sub OnStart(ByVal args() As String)
        Timer1 = New Timer()
        Me.Timer1.Interval = 60000
        AddHandler Me.Timer1.Elapsed, New System.Timers.ElapsedEventHandler(AddressOf Me.Timer1_Elapsed)
        Timer1.Enabled = True
        writeErrorLogs("service has started")
        ' Add code here to start your service. This method should set things
        ' in motion so your service can do its work.
    End Sub

    Protected Overrides Sub OnStop()
        Timer1.Enabled = False
        writeErrorLogs("service has Stopped")
        ' Add code here to perform any tear-down necessary to stop your service.
    End Sub
    Sub testWrite(mssg As String)
        Try
            Dim file As StreamWriter
            file = My.Computer.FileSystem.OpenTextFileWriter(FILE_NAME, True)
            file.WriteLine(mssg & Date.Now.ToString("dd MMMM yyyy hh:mm:sss"))
            file.Close()
        Catch ex As Exception
        End Try
    End Sub
    Private Sub Timer1_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles Timer1.Elapsed
        If Not BackgroundWorker1.IsBusy Then
            BackgroundWorker1.RunWorkerAsync()
        End If
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        If (checkForValidToken() = False) Then
            getValidToken()
        Else
            writeErrorLogs(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<")

            getProxyCredentials()
            sendSSBReg()
            'PANODA KUGADZIRISWA
            writeErrorLogs(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<")
            loopRecordsBatchID()

            writeErrorLogs(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>><<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<")
            loopResponseBatchID()
            loopCommitBatchID()
            'PANODA KUGADZIRWA
            getResponses()
            'getPayments()
            'looppaymentBatchID()

        End If
    End Sub
    Public Sub writeErrorLogs(ByVal errorMessage As String)
        Dim objWriter As StreamWriter = New StreamWriter(fileName, True)
        objWriter.WriteLine(DateTime.Now.ToString() & " :Feedback: " + errorMessage)
        objWriter.Close()
    End Sub
    Private Function checkForValidToken() As Boolean
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Using cmd = New SqlCommand("select top 1 * from [NdasendaTokens] where [TokenExpiryDate] > getdate()", con)
                    Dim ds As New DataSet
                    Dim adp = New SqlDataAdapter(cmd)
                    adp.Fill(ds, "cntrl")
                    If ds.Tables(0).Rows.Count > 0 Then
                        Scope = ds.Tables(0).Rows(0).Item("Scope")
                        Token_Type = ds.Tables(0).Rows(0).Item("Token_Type")
                        Access_Token = ds.Tables(0).Rows(0).Item("Access_Token")
                        Expires_In = ds.Tables(0).Rows(0).Item("Expires_In")
                        Refresh_Token = ds.Tables(0).Rows(0).Item("Refresh_Token")
                        TokenExpiryDate = ds.Tables(0).Rows(0).Item("TokenExpiryDate")
                        Return True
                    Else
                        Return False
                    End If
                End Using
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub getProxyCredentials()
        Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
            Using cmd = New SqlCommand("SELECT TOP  1 IPADDRESS,PORT,USERNAME,PASSWORD  FROM PROXYSERVERCONF order by  id asc", con)
                Dim ds As New DataSet
                Dim adp = New SqlDataAdapter(cmd)
                adp.Fill(ds, "cntrl")
                If ds.Tables(0).Rows.Count > 0 Then

                    Posturl = "http://" & ds.Tables(0).Rows(0).Item("IPADDRESS").ToString() & ":" & ds.Tables(0).Rows(0).Item("PORT").ToString() & "/"
                    username = ds.Tables(0).Rows(0).Item("USERNAME").ToString()
                    password = ds.Tables(0).Rows(0).Item("PASSWORD").ToString()

                    writeErrorLogs(Posturl.ToString)
                Else
                    Posturl = "http://192.168.4.7:80/"
                    username = "Redsphereadmin"
                    password = "Pass@#red1234"
                    writeErrorLogs(Posturl.ToString + "hardcoded")
                End If

            End Using
        End Using
    End Sub
    
    Private Sub getValidToken()
        ' ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
        Dim proxyServer As WebProxy
        getProxyCredentials()
        proxyServer = New WebProxy(Posturl, True)
        proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
        Try
            Dim request As HttpWebRequest
            Dim enc As UTF8Encoding
            Dim postdata As String
            Dim postdatabytes As Byte()
            request = HttpWebRequest.Create("https://ndasenda.azurewebsites.net/connect/token")
            request.Proxy = proxyServer
            enc = New System.Text.UTF8Encoding()
            postdata = "grant_type=password&username=redsphere@cbz.co.zw&password=R3done#123"
            postdatabytes = enc.GetBytes(postdata)
            request.Method = "POST"
            request.ContentType = "application/x-www-form-urlencoded"
            request.ContentLength = postdatabytes.Length
            Using stream = request.GetRequestStream()
                stream.Write(postdatabytes, 0, postdatabytes.Length)
            End Using
            Dim result = request.GetResponse()
            Dim inStream = New StreamReader(result.GetResponseStream())
            Dim responseAPI As String = inStream.ReadToEnd
            Dim response1 As NdasendaToken = New NdasendaToken()
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of NdasendaToken)(responseAPI)
            RecordResponse(response1.scope, response1.token_type, response1.access_token, response1.expires_in.ToString, response1.refresh_token)
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Public Sub RecordResponse(ByVal Scope As String, ByVal Token_Type As String, ByVal Access_Token As String, ByVal Expires_In As String, ByVal Refresh_Token As String)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim UpdateQuery As String = "INSERT INTO [dbo].[NdasendaTokens]([Scope],[Token_Type],[Access_Token],[Expires_In],[Refresh_Token],[TokenExpiryDate])VALUES (@Scope,@Token_Type,@Access_Token,Cast(@Expires_In as Int),@Refresh_Token,DATEADD(SECOND,Cast(@Expires_In as Int),GETDATE()))"
                cmd = New SqlCommand(UpdateQuery, con)
                cmd.CommandType = CommandType.Text
                cmd.Parameters.AddWithValue("@Scope", Scope)
                cmd.Parameters.AddWithValue("@Token_Type", Token_Type)
                cmd.Parameters.AddWithValue("@Access_Token", Access_Token)
                cmd.Parameters.AddWithValue("@Expires_In", Expires_In)
                cmd.Parameters.AddWithValue("@Refresh_Token", Refresh_Token)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                If cmd.ExecuteNonQuery() Then
                    Me.Refresh_Token = Refresh_Token
                    Me.Scope = Scope
                    Me.Token_Type = Token_Type
                    Me.Access_Token = Access_Token
                    Me.Expires_In = Expires_In
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Public Sub getResponses()
        Try
            ' System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
            Dim proxyServer As WebProxy
            getProxyCredentials()
            proxyServer = New WebProxy(Posturl, True)
            proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
            Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/responses/" + DateTime.Now.ToString("yyyyMMdd") + "/" + DateTime.Now.ToString("yyyyMMdd") + "/800083436")
            'Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/responses/20210818/20210823/800083436")
            Dim request As WebRequest = WebRequest.Create(myUri)
            request.Proxy = proxyServer
            request.Headers.Add("Authorization", "Bearer " & Access_Token)
            request.ContentType = "application/json"
            request.PreAuthenticate = True
            request.Method = "GET"
            Dim responseFromServer As String = ""
            Using response As WebResponse = request.GetResponse()
                Using stream As Stream = response.GetResponseStream()
                    Dim reader As StreamReader = New StreamReader(stream)
                    responseFromServer = reader.ReadToEnd()
                End Using
            End Using
            'writeErrorLogs(responseFromServer)
            Dim response1() As GetResponses
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of GetResponses())(responseFromServer.ToString)
            For index As Integer = 0 To response1.Length - 1
                If (checkresponseidExistance(response1(index).id.ToString) = True) Then
                ElseIf (checkresponseidExistance(response1(index).id.ToString) = False) Then
                    Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                        Dim InsertQuery As String = "insert into SSBDeductionResponses (id,deductionCode,recordsCount,totalAmount,creationDate) values ('" + response1(index).id.ToString + "','" + response1(index).deductionCode.ToString + "','" + response1(index).recordsCount.ToString + "','" + response1(index).totalAmount.ToString + "','" + response1(index).creationDate.ToString + "')"
                        cmd = New SqlCommand(InsertQuery, con)
                        If con.State = ConnectionState.Open Then
                            con.Close()
                        End If
                        con.Open()
                        cmd.ExecuteNonQuery()
                        con.Close()
                    End Using
                End If
            Next
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Function checkresponseidExistance(ByVal id As String) As Boolean
        Dim existance As Boolean = False
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim SelectQuery As String = " select count(*) from SSBDeductionResponses where id='" + id + "' "
                cmd = New SqlCommand(SelectQuery, con)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If count >= 1 Then
                    existance = True
                ElseIf count = 0 Then
                    existance = False
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
        Return existance
    End Function

    Public Sub getPayments()
        Try
            ' System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
            Dim proxyServer As WebProxy
            getProxyCredentials()
            proxyServer = New WebProxy(Posturl, True)
            ' proxyServer.Credentials = New Net.NetworkCredential("Redsphereadmin", "Pass@word1", "cbz")
            Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/payments/" + DateTime.Now.ToString("yyyyMMdd") + "/" + DateTime.Now.ToString("yyyyMMdd") + "/800083436")
            ' Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/payments/20210818/20210823/800083436")
            Dim request As WebRequest = WebRequest.Create(myUri)
            request.Proxy = proxyServer
            request.Headers.Add("Authorization", "Bearer " & Access_Token)
            request.ContentType = "application/json"
            request.PreAuthenticate = True
            request.Method = "GET"
            Dim responseFromServer As String = ""
            Using response As WebResponse = request.GetResponse()
                Using stream As Stream = response.GetResponseStream()
                    Dim reader As StreamReader = New StreamReader(stream)
                    responseFromServer = reader.ReadToEnd()
                End Using
            End Using
            Dim response1() As GetPayments
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of GetPayments())(responseFromServer.ToString)
            For index As Integer = 0 To response1.Length - 1
                If (checkpaymentidExistance(response1(index).id.ToString) = True) Then
                ElseIf (checkpaymentidExistance(response1(index).id.ToString) = False) Then
                    Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                        Dim InsertQuery As String = "insert into SSBDeductionPayments (id,deductionCode,creationDate,recordsCount,totalAmount) values ('" + response1(index).id.ToString + "','" + response1(index).deductionCode.ToString + "','" + response1(index).creationDate.ToString + "','" + response1(index).recordsCount.ToString + "','" + response1(index).totalAmount.ToString + "')"
                        cmd = New SqlCommand(InsertQuery, con)
                        If con.State = ConnectionState.Open Then
                            con.Close()
                        End If
                        con.Open()
                        cmd.ExecuteNonQuery()
                        con.Close()
                    End Using
                End If
            Next
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub

    Function checkpaymentidExistance(ByVal id As String) As Boolean
        Dim existance As Boolean = False
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim SelectQuery As String = " select count(*) from SSBDeductionPayments where id='" + id + "' "
                cmd = New SqlCommand(SelectQuery, con)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If count >= 1 Then
                    existance = True
                ElseIf count = 0 Then
                    existance = False
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
        Return existance
    End Function

    Public Sub looppaymentBatchID()
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                con.Open()
                Using cmd As New SqlCommand("select id from SSBDeductionPayments order by id desc", con)
                    Dim da = New SqlDataAdapter(cmd)
                    Dim ds As New DataSet("ds")
                    da.Fill(ds)
                    Dim dt As DataTable = ds.Tables(0)

                    If dt.Rows.Count > 0 Then
                        For Each DRow As DataRow In dt.Rows
                            DRow.Item("id").ToString()
                            getPaymentRecords(DRow.Item("id").ToString())
                        Next
                    Else
                    End If
                End Using
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
    End Sub
    Public Sub getPaymentRecords(ByVal id As String)
        Try
            ' System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
            Dim proxyServer As WebProxy
            proxyServer = New WebProxy(Posturl, True)
            proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
            Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/payments/" + id + "")
            ' Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/payments/PAY21081B85")
            Dim request As WebRequest = WebRequest.Create(myUri)
            request.Proxy = proxyServer
            request.Headers.Add("Authorization", "Bearer " & Access_Token)
            request.ContentType = "application/json"
            request.PreAuthenticate = True
            request.Method = "Get"
            Dim responseFromServer As String = ""
            Using response As WebResponse = request.GetResponse()
                Using stream As Stream = response.GetResponseStream()
                    Dim reader As StreamReader = New StreamReader(stream)
                    responseFromServer = reader.ReadToEnd()
                End Using
            End Using
            'writeErrorLogs(responseFromServer)
            Dim response1 As GetPaymentRecords
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of GetPaymentRecords)(responseFromServer.ToString)
            For index As Integer = 0 To response1.recordsCount - 1
                If (checkpaymentExistance(response1.records(index).idNumber.ToString, response1.records(index).ecNumber.ToString, response1.records(index).reference.ToString, response1.records(index).transDate.ToString, response1.records(index).amount.ToString) = True) Then
                ElseIf (checkpaymentExistance(response1.records(index).idNumber.ToString, response1.records(index).ecNumber.ToString, response1.records(index).reference.ToString, response1.records(index).transDate.ToString, response1.records(index).amount.ToString) = False) Then
                    RecordPaymentRecords(response1.records(index).id, response1.records(index).idNumber, response1.records(index).ecNumber, response1.records(index).reference, response1.records(index).transDate, response1.records(index).amount)
                End If
            Next
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Public Sub RecordPaymentRecords(ByVal id As String, ByVal idNumber As String, ByVal ecNumber As String, ByVal reference As String, ByVal transDate As String, ByVal amount As String)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim UpdateQuery As String = "INSERT INTO [dbo].[SSBPaymentRecords] ([NdasendaID],[idNumber],[ecNumber],[reference],[transDate],[amount])VALUES (@id ,@idNumber ,@ecNumber ,@reference  ,Convert(CHAR(8),@transDate,112) ,CAST (cast(@amount as decimal) / cast(100.00 as decimal) As money))"
                cmd = New SqlCommand(UpdateQuery, con)
                cmd.CommandType = CommandType.Text
                cmd.Parameters.AddWithValue("@id", id)
                cmd.Parameters.AddWithValue("@idNumber", idNumber)
                cmd.Parameters.AddWithValue("@ecNumber", ecNumber)
                cmd.Parameters.AddWithValue("@reference", reference)
                cmd.Parameters.AddWithValue("@transDate", transDate)
                cmd.Parameters.AddWithValue("@amount", amount)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                If cmd.ExecuteNonQuery() Then
                    Me.id = id
                    Me.idnumber = idNumber
                    Me.ecnumber = ecNumber
                    Me.reference = reference
                    Me.transdate = transDate
                    Me.amount = amount
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Function checkpaymentExistance(ByVal idNumber As String, ByVal ecNumber As String, ByVal reference As String, ByVal transDate As String, ByVal amount As String) As Boolean
        Dim existance As Boolean = False
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim SelectQuery As String = "select count(*) from SSBPaymentRecords where idNumber='" + idNumber + "' and ecNumber='" + ecNumber + "' and reference='" + reference + "' and transDate='" + transDate + "' and (amount*100)='" + amount + "' "
                cmd = New SqlCommand(SelectQuery, con)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If count >= 1 Then
                    existance = True
                    Return True
                ElseIf count = 0 Then
                    existance = False
                    Return False
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
        Return existance
    End Function
    Public Sub sendSSBReg()
        Dim builder As New System.Text.StringBuilder
        Dim json, mainData As String
        Dim totalAmount As Decimal
        Dim listOfAllSentToNdasenda As New List(Of Integer)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Using cmd = New SqlCommand("[dbo].[allLOansToSendToNdasenda]  ", con)
                    Dim ds As New DataSet
                    Dim adp = New SqlDataAdapter(cmd)
                    adp.Fill(ds, "cntrl")
                    If ds.Tables(0).Rows.Count > 0 Then
                        For index As Integer = 0 To ds.Tables(0).Rows.Count - 1
                            listOfAllSentToNdasenda.Add(ds.Tables(0).Rows(index).Item("reference"))
                            json = "{""idNumber"": """ + ds.Tables(0).Rows(index).Item("IDNO").ToString + """,""ecNumber"": """ + ds.Tables(0).Rows(index).Item("ECNO").ToString + """,""type"": """ + ds.Tables(0).Rows(index).Item("type").ToString + """,""reference"": """ + ds.Tables(0).Rows(index).Item("reference").ToString + """,""startDate"": """ + ds.Tables(0).Rows(index).Item("StartDate").ToString + """,""endDate"": """ + ds.Tables(0).Rows(index).Item("EndDate").ToString + """,""name"": """ + ds.Tables(0).Rows(index).Item("FORENAMES").ToString + """,""surname"": """ + ds.Tables(0).Rows(index).Item("SURNAME").ToString + """,""amount"":" + ds.Tables(0).Rows(index).Item("Payment").ToString + ",""totalAmount"":" + ds.Tables(0).Rows(index).Item("total").ToString + "}"
                            builder.Append(json)
                            If index <> (ds.Tables(0).Rows.Count - 1) Then
                                builder.Append(",")
                            End If
                            totalAmount += CDec(ds.Tables(0).Rows(index).Item("Payment").ToString())
                            'updateQuestApplicationWithStatus(ds.Tables(0).Rows(index).Item("ID"))
                            'updateNdasendaChanges(ds.Tables(0).Rows(index).Item("ID"))

                        Next
                        mainData = "{""recordsCount"": " + ds.Tables(0).Rows.Count.ToString + ",""totalAmount"": " + totalAmount.ToString() + ",""securityToken"": ""110423"",""deductionCode"": ""800083436"",""status"": ""DRAFT"",""records"": [" + builder.ToString + "]}"
                        writeErrorLogs(mainData)
                        ' ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
                        Dim proxyServer As WebProxy
                        getProxyCredentials()
                        proxyServer = New WebProxy(Posturl, True)
                        proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
                        writeErrorLogs(Posturl + username + password)
                        Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/requests")
                        Dim request As HttpWebRequest = WebRequest.Create(myUri)
                        request.Proxy = proxyServer
                        Dim bytes As Byte()
                        bytes = System.Text.Encoding.ASCII.GetBytes(mainData.ToString)
                        request.Headers.Add("Authorization", "Bearer " + Access_Token)
                        'request.PreAuthenticate = True
                        request.ContentType = "application/json"
                        request.ContentLength = bytes.Length
                        request.Method = "POST"
                        Dim requestStream As Stream = request.GetRequestStream()
                        requestStream.Write(bytes, 0, bytes.Length)
                        requestStream.Close()
                        Dim responseFromServer1 As String = ""
                        Using response As HttpWebResponse = request.GetResponse()
                            If (response.StatusCode = HttpStatusCode.Accepted Or response.StatusCode = HttpStatusCode.OK Or response.StatusCode = HttpStatusCode.Created) Then

                                For Each loanSentToNdasenda As String In listOfAllSentToNdasenda
                                    updateQuestApplicationWithStatus(loanSentToNdasenda)
                                    updateNdasendaChanges(loanSentToNdasenda)
                                Next
                                Using stream As Stream = response.GetResponseStream()
                                    Dim reader As StreamReader = New StreamReader(stream)
                                    responseFromServer1 = reader.ReadToEnd()
                                End Using

                                Dim response1 As PostDeductions
                                Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
                                response1 = ser.Deserialize(Of PostDeductions)(responseFromServer1.ToString)
                                'response1.id.ToString()
                                'response1.recordsCount.ToString()
                                'response1.totalAmount.ToString()
                                'response1.deductionCode.ToString()
                                'response1.status.ToString()
                                RecordRegResponse(response1.id, response1.recordsCount, response1.totalAmount, response1.deductionCode.ToString, response1.status, response1.creationDate)
                            Else
                                writeErrorLogs("THE REQUEST HAD THE RESPONSE " + response.StatusCode.ToString)

                            End If
                        End Using
                    End If
                End Using
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Private Sub updateNdasendaChanges(LoanID As String)
        Using con2 As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
            Dim UpdateQuery As String = "update NdasendaChanges set isPosted=@isposted where LOANID=@id"
            cmd = New SqlCommand(UpdateQuery, con2)
            cmd.CommandType = CommandType.Text
            cmd.Parameters.AddWithValue("@id", LoanID)
            cmd.Parameters.AddWithValue("@isposted", "1")
            If con2.State = ConnectionState.Open Then
                con2.Close()
            End If
            con2.Open()
            cmd.ExecuteNonQuery()
            con2.Close()

        End Using
    End Sub

    Private Sub updateQuestApplicationWithStatus(LoanID As String)
        Using con2 As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
            Dim UpdateQuery As String = "update QUEST_APPLICATION set isPosted=@isposted where ID=@id"
            cmd = New SqlCommand(UpdateQuery, con2)
            cmd.CommandType = CommandType.Text
            cmd.Parameters.AddWithValue("@id", LoanID)
            cmd.Parameters.AddWithValue("@isposted", "1")
            If con2.State = ConnectionState.Open Then
                con2.Close()
            End If
            con2.Open()
            cmd.ExecuteNonQuery()
            con2.Close()
        End Using
    End Sub
    Public Sub RecordRegResponse(ByVal id As String, ByVal recordsCount As String, ByVal totalAmount As String, ByVal deductionCode As String, ByVal status As String, ByVal creationDate As Date)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim UpdateQuery As String = "INSERT INTO [dbo].[SSBRegResponse]([id],[recordsCount],[totalAmount],[deductionCode],[status],[creationDate])VALUES (@id,@recordsCount,@totalAmount,@deductionCode,@status,@creationDate)"
                cmd = New SqlCommand(UpdateQuery, con)
                cmd.CommandType = CommandType.Text
                cmd.Parameters.AddWithValue("@id", id)
                cmd.Parameters.AddWithValue("@recordsCount", recordsCount)
                cmd.Parameters.AddWithValue("@totalAmount", totalAmount)
                cmd.Parameters.AddWithValue("@deductionCode", deductionCode)
                cmd.Parameters.AddWithValue("@status", status)
                cmd.Parameters.AddWithValue("@creationDate", creationDate)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                If cmd.ExecuteNonQuery() Then
                    Me.id = id
                    Me.recordsCount = recordsCount
                    Me.totalAmount = totalAmount
                    Me.deductionCode = deductionCode
                    Me.status = status
                    Me.creationDate = creationDate
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Public Sub loopRecordsBatchID()
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                con.Open()
                Using cmd As New SqlCommand("select id from SSBRegResponse order by id desc", con)
                    Dim da = New SqlDataAdapter(cmd)
                    Dim ds As New DataSet("ds")
                    da.Fill(ds)
                    Dim dt As DataTable = ds.Tables(0)

                    If dt.Rows.Count > 0 Then
                        For Each DRow As DataRow In dt.Rows
                            DRow.Item("id").ToString()
                            getRecords(DRow.Item("id").ToString())
                        Next
                    Else
                    End If
                End Using
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
    End Sub
    Public Sub loopResponseBatchID()
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                con.Open()
                Using cmd As New SqlCommand("select id from SSBRegResponse order by id desc", con)
                    Dim da = New SqlDataAdapter(cmd)
                    Dim ds As New DataSet("ds")
                    da.Fill(ds)
                    Dim dt As DataTable = ds.Tables(0)

                    If dt.Rows.Count > 0 Then
                        For Each DRow As DataRow In dt.Rows
                            DRow.Item("id").ToString()
                            getResponseRecords(DRow.Item("id").ToString())
                        Next
                    Else
                    End If
                End Using
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
    End Sub
    Public Sub loopCommitBatchID()
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                con.Open()
                Using cmd As New SqlCommand("select id from SSBRegResponse order by id desc", con)
                    Dim da = New SqlDataAdapter(cmd)
                    Dim ds As New DataSet("ds")
                    da.Fill(ds)
                    Dim dt As DataTable = ds.Tables(0)

                    If dt.Rows.Count > 0 Then
                        For Each DRow As DataRow In dt.Rows
                            DRow.Item("id").ToString()
                            commitDeductions(DRow.Item("id").ToString())
                        Next
                    Else
                    End If
                End Using
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try

    End Sub

    Public Sub commitDeductions(ByVal id As String)
        Try
            ' System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
            Dim proxyServer As WebProxy
            proxyServer = New WebProxy(Posturl, True)
            proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
            Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/requests/commit/" + id + "")
            Dim request As WebRequest = WebRequest.Create(myUri)
            request.Proxy = proxyServer
            Dim bytes As Byte()
            bytes = System.Text.Encoding.ASCII.GetBytes(id.ToString)
            request.Headers.Add("Authorization", "Bearer " + Access_Token)
            request.PreAuthenticate = True
            request.ContentType = "application/json"
            request.ContentLength = bytes.Length
            request.Method = "POST"
            Dim requestStream As Stream = request.GetRequestStream()
            requestStream.Write(bytes, 0, bytes.Length)
            requestStream.Close()
            Dim responseFromServer As String = ""
            Using response As WebResponse = request.GetResponse()
                Using stream As Stream = response.GetResponseStream()
                    Dim reader As StreamReader = New StreamReader(stream)
                    responseFromServer = reader.ReadToEnd()
                End Using
            End Using
            '  writeErrorLogs(responseFromServer)
            Dim response1 As GetRecords
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of GetRecords)(responseFromServer.ToString)
            For index As Integer = 0 To response1.recordsCount - 1
                If (checkcommitExistance(response1.id) = True) Then
                ElseIf (checkcommitExistance(response1.id) = False) Then
                    RecordCommitResponse(response1.id, response1.recordsCount, response1.totalAmount, response1.deductionCode.ToString, response1.status, response1.creationDate)
                End If
            Next
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub

    Public Sub RecordCommitResponse(ByVal id As String, ByVal recordsCount As String, ByVal totalAmount As String, ByVal deductionCode As String, ByVal status As String, ByVal creationDate As Date)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim UpdateQuery As String = "INSERT INTO [dbo].[SSBCommitResponse]([id],[recordsCount],[totalAmount],[deductionCode],[status],[creationDate])VALUES (@id,@recordsCount,@totalAmount,@deductionCode,@status,@creationDate)"
                cmd = New SqlCommand(UpdateQuery, con)
                cmd.CommandType = CommandType.Text
                cmd.Parameters.AddWithValue("@id", id)
                cmd.Parameters.AddWithValue("@recordsCount", recordsCount)
                cmd.Parameters.AddWithValue("@totalAmount", totalAmount)
                cmd.Parameters.AddWithValue("@deductionCode", deductionCode)
                cmd.Parameters.AddWithValue("@status", status)
                cmd.Parameters.AddWithValue("@creationDate", creationDate)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                If cmd.ExecuteNonQuery() Then
                    Me.id = id
                    Me.recordsCount = recordsCount
                    Me.totalAmount = totalAmount
                    Me.deductionCode = deductionCode
                    Me.status = status
                    Me.creationDate = creationDate
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Function checkcommitExistance(ByVal id As String) As Boolean
        Dim existance As Boolean = False
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim SelectQuery As String = " select count(*) from SSBCommitResponse where id='" + id + "' "
                cmd = New SqlCommand(SelectQuery, con)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If count >= 1 Then
                    existance = True
                ElseIf count = 0 Then
                    existance = False
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
        Return existance
    End Function

    Public Sub getRecords(ByVal id As String)
        Try
            ' System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
            Dim proxyServer As WebProxy
            proxyServer = New WebProxy(Posturl, True)
            proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
            Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/requests/" + id + "")
            Dim request As WebRequest = WebRequest.Create(myUri)
            request.Proxy = proxyServer
            request.Headers.Add("Authorization", "Bearer " & Access_Token)
            request.ContentType = "application/json"
            request.PreAuthenticate = True
            request.Method = "Get"
            Dim responseFromServer As String = ""
            Using response As WebResponse = request.GetResponse()
                Using stream As Stream = response.GetResponseStream()
                    Dim reader As StreamReader = New StreamReader(stream)
                    responseFromServer = reader.ReadToEnd()
                End Using
            End Using
            ' writeErrorLogs(responseFromServer)
            Dim response1 As GetRecords
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of GetRecords)(responseFromServer.ToString)
            For index As Integer = 0 To response1.recordsCount - 1
                If response1.records(index).type.ToString = "New" Then
                    If (checkrecordExistance(response1.records(index).idNumber.ToString, response1.records(index).ecNumber.ToString, response1.records(index).reference.ToString, response1.records(index).type.ToString, response1.records(index).startDate.ToString, response1.records(index).endDate.ToString, response1.records(index).amount.ToString) = True) Then
                    ElseIf (checkrecordExistance(response1.records(index).idNumber.ToString, response1.records(index).ecNumber.ToString, response1.records(index).reference.ToString, response1.records(index).type.ToString, response1.records(index).startDate.ToString, response1.records(index).endDate.ToString, response1.records(index).amount.ToString) = False) Then
                        RecordRegRecords(response1.records(index).id, response1.records(index).idNumber, response1.records(index).ecNumber, response1.records(index).type, response1.records(index).reference, response1.records(index).startDate, response1.records(index).endDate, response1.records(index).amount)
                    End If
                Else
                    If (checkrecordExistance(response1.records(index).idNumber.ToString, response1.records(index).ecNumber.ToString, response1.records(index).reference.ToString, response1.records(index).type.ToString, response1.records(index).startDate.ToString, response1.records(index).endDate.ToString, response1.records(index).amount.ToString) = True) Then
                    ElseIf (checkrecordExistance(response1.records(index).idNumber.ToString, response1.records(index).ecNumber.ToString, response1.records(index).reference.ToString, response1.records(index).type.ToString, response1.records(index).startDate.ToString, response1.records(index).endDate.ToString, response1.records(index).amount.ToString) = False) Then
                        RecordRegRecords(response1.records(index).id, response1.records(index).idNumber, response1.records(index).ecNumber, response1.records(index).type, response1.records(index).reference, response1.records(index).startDate, response1.records(index).endDate, response1.records(index).amount)
                    End If
                End If
            Next
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Public Sub RecordRegRecords(ByVal id As String, ByVal idNumber As String, ByVal ecNumber As String, ByVal type As String, ByVal reference As String, ByVal startDate As String, ByVal endDate As String, ByVal amount As String)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim UpdateQuery As String = "INSERT INTO [dbo].[SSBRegRecords] ([id] ,[idNumber] ,[ecNumber] ,[type] ,[reference] ,[startDate] ,[endDate] ,[amount]) VALUES(@id,@idNumber ,@ecNumber ,@type ,@reference , Convert(CHAR(8),@startDate,112),Convert(CHAR(8),@endDate,112),@amount)"
                cmd = New SqlCommand(UpdateQuery, con)
                cmd.CommandType = CommandType.Text
                cmd.Parameters.AddWithValue("@id", id)
                cmd.Parameters.AddWithValue("@idNumber", idNumber)
                cmd.Parameters.AddWithValue("@ecNumber", ecNumber)
                cmd.Parameters.AddWithValue("@type", type)
                cmd.Parameters.AddWithValue("@reference", reference)
                cmd.Parameters.AddWithValue("@startDate", startDate)
                cmd.Parameters.AddWithValue("@endDate", endDate)
                cmd.Parameters.AddWithValue("@amount", amount)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                If cmd.ExecuteNonQuery() Then
                    Me.id = id
                    Me.idnumber = idNumber
                    Me.ecnumber = ecNumber
                    Me.type = type
                    Me.reference = reference
                    Me.startdate = startDate
                    Me.enddate = endDate
                    Me.amount = amount
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Function checkrecordExistance(ByVal idNumber As String, ByVal ecNumber As String, ByVal reference As String, ByVal type As String, ByVal startDate As String, ByVal endDate As String, ByVal amount As String) As Boolean
        Dim existance As Boolean = False
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim SelectQuery As String = " select count(*) from SSBRegRecords where idNumber='" + idNumber + "' and ecNumber='" + ecNumber + "' and reference='" + reference + "' and type='" + type + "' and startDate='" + startDate + "' and endDate='" + endDate + "' and amount='" + amount + "' "
                cmd = New SqlCommand(SelectQuery, con)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If count >= 1 Then
                    existance = True
                ElseIf count = 0 Then
                    existance = False
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
        Return existance
    End Function

    Public Sub getResponseRecords(ByVal id As String)
        Try
            ' System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(se As Object, cert As System.Security.Cryptography.X509Certificates.X509Certificate, chain As System.Security.Cryptography.X509Certificates.X509Chain, sslerror As System.Net.Security.SslPolicyErrors) True
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 Or SecurityProtocolType.Tls11 Or SecurityProtocolType.Tls
            Dim proxyServer As WebProxy
            proxyServer = New WebProxy(Posturl, True)
            proxyServer.Credentials = New Net.NetworkCredential(username, password, "cbz")
            Dim myUri As New Uri("https://ndasenda.azurewebsites.net/api/v1/deductions/responses/" + id + "")
            Dim request As WebRequest = WebRequest.Create(myUri)
            request.Proxy = proxyServer
            request.Headers.Add("Authorization", "Bearer " & Access_Token)
            request.ContentType = "application/json"
            request.PreAuthenticate = True
            request.Method = "Get"
            Dim responseFromServer As String = ""
            Using response As WebResponse = request.GetResponse()
                Using stream As Stream = response.GetResponseStream()
                    Dim reader As StreamReader = New StreamReader(stream)
                    responseFromServer = reader.ReadToEnd()
                End Using
            End Using
            ' writeErrorLogs(responseFromServer)
            Dim response1() As GetResponseRecords
            Dim ser As JavaScriptSerializer = New JavaScriptSerializer()
            response1 = ser.Deserialize(Of GetResponseRecords())(responseFromServer.ToString)
            For index As Integer = 0 To response1(0).recordsCount - 1
                If response1(0).records(index).type = "NEW" Then
                    If (checkresponseExistance(response1(0).records(index).idNumber.ToString, response1(0).records(index).ecNumber.ToString, response1(0).records(index).type.ToString, response1(0).records(index).reference.ToString, response1(0).records(index).amount.ToString, response1(0).records(index).status.ToString) = True) Then
                    ElseIf (checkresponseExistance(response1(0).records(index).idNumber.ToString, response1(0).records(index).ecNumber.ToString, response1(0).records(index).type.ToString, response1(0).records(index).reference.ToString, response1(0).records(index).amount.ToString, response1(0).records(index).status.ToString) = False) Then
                        RecordResponseRecords(response1(0).records(index).id, response1(0).records(index).idNumber, response1(0).records(index).ecNumber, response1(0).records(index).type, response1(0).records(index).reference, response1(0).records(index).amount, response1(0).records(index).name, response1(0).records(index).branch, response1(0).records(index).bankAccount, response1(0).records(index).status, response1(0).records(index).message)
                    End If
                Else
                    If (checkresponseExistance(response1(0).records(index).idNumber.ToString, response1(0).records(index).ecNumber.ToString, response1(0).records(index).type.ToString, response1(0).records(index).reference.ToString, response1(0).records(index).amount.ToString, response1(0).records(index).status.ToString) = True) Then
                    ElseIf (checkresponseExistance(response1(0).records(index).idNumber.ToString, response1(0).records(index).ecNumber.ToString, response1(0).records(index).type.ToString, response1(0).records(index).reference.ToString, response1(0).records(index).amount.ToString, response1(0).records(index).status.ToString) = False) Then
                        RecordResponseRecords(response1(0).records(index).id, response1(0).records(index).idNumber, response1(0).records(index).ecNumber, response1(0).records(index).type, response1(0).records(index).reference, response1(0).records(index).amount, response1(0).records(index).name, response1(0).records(index).branch, response1(0).records(index).bankAccount, response1(0).records(index).status, response1(0).records(index).message)
                    End If
                End If
            Next
            For index As Integer = 0 To response1(1).recordsCount - 1
                If response1(1).records(index).type = "NEW" Then
                    If (checkresponseExistance(response1(1).records(index).idNumber.ToString, response1(1).records(index).ecNumber.ToString, response1(1).records(index).type.ToString, response1(1).records(index).reference.ToString, response1(1).records(index).amount.ToString, response1(1).records(index).status.ToString) = True) Then
                    ElseIf (checkresponseExistance(response1(1).records(index).idNumber.ToString, response1(1).records(index).ecNumber.ToString, response1(1).records(index).type.ToString, response1(1).records(index).reference.ToString, response1(1).records(index).amount.ToString, response1(1).records(index).status.ToString) = False) Then
                        RecordResponseRecords(response1(1).records(index).id, response1(1).records(index).idNumber, response1(1).records(index).ecNumber, response1(1).records(index).type, response1(1).records(index).reference, response1(1).records(index).amount, response1(1).records(index).name, response1(1).records(index).branch, response1(1).records(index).bankAccount, response1(1).records(index).status, response1(1).records(index).message)
                    End If
                Else
                    If (checkresponseExistance(response1(1).records(index).idNumber.ToString, response1(1).records(index).ecNumber.ToString, response1(1).records(index).type.ToString, response1(1).records(index).reference.ToString, response1(1).records(index).amount.ToString, response1(1).records(index).status.ToString) = True) Then
                    ElseIf (checkresponseExistance(response1(1).records(index).idNumber.ToString, response1(1).records(index).ecNumber.ToString, response1(1).records(index).type.ToString, response1(1).records(index).reference.ToString, response1(1).records(index).amount.ToString, response1(1).records(index).status.ToString) = False) Then
                        RecordResponseRecords(response1(1).records(index).id, response1(1).records(index).idNumber, response1(1).records(index).ecNumber, response1(1).records(index).type, response1(1).records(index).reference, response1(1).records(index).amount, response1(1).records(index).name, response1(1).records(index).branch, response1(1).records(index).bankAccount, response1(1).records(index).status, response1(1).records(index).message)
                    End If
                End If
            Next
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Public Sub RecordResponseRecords(ByVal id As String, ByVal idNumber As String, ByVal ecNumber As String, ByVal type As String, ByVal reference As String, ByVal amount As String, ByVal name As String, ByVal branch As String, ByVal bankAccount As String, ByVal status As String, ByVal message As String)
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim UpdateQuery As String = "INSERT INTO [dbo].[SSBResponseRecords]([id],[idnumber],[ecnumber],[type],[reference],[amount],[name],[branch],[bankAccount],[status],[message])VALUES(@id,@idnumber,@ecnumber,@type,@reference,CAST (cast(@amount as decimal) / cast(100.00 as decimal) As money),@name,@branch,@bankAccount,@status,@message)"
                cmd = New SqlCommand(UpdateQuery, con)
                cmd.CommandType = CommandType.Text
                cmd.Parameters.AddWithValue("@id", id)
                cmd.Parameters.AddWithValue("@idNumber", idNumber)
                cmd.Parameters.AddWithValue("@ecNumber", ecNumber)
                cmd.Parameters.AddWithValue("@type", type)
                cmd.Parameters.AddWithValue("@reference", reference)
                cmd.Parameters.AddWithValue("@amount", amount)
                If String.IsNullOrEmpty(name) Then
                    cmd.Parameters.AddWithValue("@name", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@name", name)
                End If
                If String.IsNullOrEmpty(branch) Then
                    cmd.Parameters.AddWithValue("@branch", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@branch", branch)
                End If
                If String.IsNullOrEmpty(bankAccount) Then
                    cmd.Parameters.AddWithValue("@bankAccount", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@bankAccount", bankAccount)
                End If
                cmd.Parameters.AddWithValue("@status", status)
                If String.IsNullOrEmpty(message) Then
                    cmd.Parameters.AddWithValue("@message", DBNull.Value)
                Else
                    cmd.Parameters.AddWithValue("@message", message)
                End If
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                If cmd.ExecuteNonQuery() Then
                    Me.id = id
                    Me.idnumber = idNumber
                    Me.ecnumber = ecNumber
                    Me.type = type
                    Me.reference = reference
                    Me.amount = amount
                    Me.name = name
                    Me.branch = branch
                    Me.bankAccount = bankAccount
                    Me.status = status
                    Me.message = message
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString)
        End Try
    End Sub
    Function checkresponseExistance(ByVal idNumber As String, ByVal ecNumber As String, ByVal type As String, ByVal reference As String, ByVal amount As String, ByVal status As String) As Boolean
        Dim existance As Boolean = False
        Try
            Using con As New SqlConnection(ConfigurationManager.ConnectionStrings("Constring2").ConnectionString)
                Dim SelectQuery As String = " select count(*) from SSBResponseRecords where idNumber='" + idNumber + "'  and ecNumber='" + ecNumber + "' and type='" + type + "' and reference='" + reference + "' and (amount*100)= '" + amount + "' and status='" + status + "' "
                cmd = New SqlCommand(SelectQuery, con)
                If con.State = ConnectionState.Open Then
                    con.Close()
                End If
                con.Open()
                Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If count >= 1 Then
                    existance = True
                    Return True
                ElseIf count = 0 Then
                    existance = False
                    Return False
                End If
                con.Close()
            End Using
        Catch ex As Exception
            writeErrorLogs(ex.ToString())
        End Try
        Return existance
    End Function
End Class





