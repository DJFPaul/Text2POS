Imports System.IO
Imports System.Drawing.Printing
Imports System.Runtime.InteropServices
Public Class Form1
    Dim PrinterName As String = ""
    Dim PrintContent
    Dim CurrentMode As Integer = 0
    Dim WasPrinted As Integer = 0
    'Const ESC = Chr(&H1B)
    'Const FS = Chr(&H1C)
    'Const GS = Chr(&H1D)

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Load settings.
        PrinterName = My.Settings.PrinterName
        If My.Settings.LineMode = 0 Then CheckBox3.Checked = False
        If My.Settings.LineMode = 1 Then CheckBox3.Checked = True
        Try
            ComboBox1.SelectedIndex = My.Settings.CharLimit
        Catch
            ComboBox1.SelectedIndex = 2
            My.Settings.CharLimit = 2
            My.Settings.Save()
        End Try
        TextBox2.Text = PrinterName

        'Check for and process startup parameters for command line use.
        Dim parameter341() As String = Environment.GetCommandLineArgs().ToArray
        Dim Parameter As String
        If (parameter341.Count - 1) >= 1 Then
            For i = 1 To parameter341.Count - 1
                Parameter = Parameter + (parameter341(i)) + " "
            Next
            For Each c As Char In Parameter
                Try
                    If TextBox1.Text.Length < Parameter.Length - 1 Then TextBox1.Text = TextBox1.Text & c
                Catch
                End Try
            Next
            'Add the processed text to the RichTextBox.
            RichTextBox1.AppendText(ProcessText(TextBox1.Text))
            RichTextBox1.SelectionStart = Len(RichTextBox1.Text)
            RichTextBox1.ScrollToCaret()
            TextBox1.Text = ""
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'Process the content of RichTextBox1 line by line into a single printable string, except if direct mode is on.
        PrintContent = ""
        If CheckBox1.Checked = False Then
            For LN As Integer = 0 To RichTextBox1.Lines.Length - 1
                PrintContent = PrintContent & RichTextBox1.Lines(LN) & vbNewLine
            Next
        End If

        'Check linecount of PrintContent and add extra blank lines to have a long enough print for easy removal.
        If RichTextBox1.Lines.Count = 0 Then PrintContent = PrintContent & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine
        If RichTextBox1.Lines.Count = 1 Then PrintContent = PrintContent & vbNewLine & vbNewLine & vbNewLine & vbNewLine & vbNewLine
        If RichTextBox1.Lines.Count = 2 Then PrintContent = PrintContent & vbNewLine & vbNewLine & vbNewLine & vbNewLine
        If RichTextBox1.Lines.Count = 3 Then PrintContent = PrintContent & vbNewLine & vbNewLine & vbNewLine
        If RichTextBox1.Lines.Count = 4 Then PrintContent = PrintContent & vbNewLine & vbNewLine
        If RichTextBox1.Lines.Count = 5 Then PrintContent = PrintContent & vbNewLine
        PrintContent = PrintContent & vbNewLine & "--------------------------------" & vbNewLine & vbNewLine

        'Send data to printer.
        Try
            RAWPOSPrinter.SendStringToPrinter(PrinterName, PrintContent)
            WasPrinted = 1
        Catch EX As Exception
            MsgBox(EX.Message)
        End Try
        PrintContent = ""
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged
        'Change colour to red if the char count of the input exceeds the max line lenght of the printer.
        If LetterReplace(TextBox1.Text).Length > ComboBox1.SelectedItem Then
            TextBox1.BackColor = Color.Red
        Else
            TextBox1.BackColor = Color.White
        End If

        'Check if the text of the RichTextBox was printed, and if yes clear the RichTextBox when the user starts typing new text.
        If WasPrinted = 1 Then
            WasPrinted = 0
            RichTextBox1.Text = ""
        End If
        If CurrentMode = 0 And CheckBox1.Checked = True Then
            RichTextBox1.Text = ""
        End If
        If CurrentMode = 1 And CheckBox1.Checked = False Then
            RichTextBox1.Text = ""
        End If
        If CheckBox1.Checked = True Then
            CurrentMode = 1
        Else
            CurrentMode = 0
        End If
    End Sub

    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.KeyCode = Keys.Enter Then
            'Once the user pressed enter, processes the text.
            Dim ProcessedText As String = ProcessText(TextBox1.Text)

            'Check if direct mode is enabled, when true pint the processed line directly.
            If CheckBox1.Checked = True Then
                Try
                    RAWPOSPrinter.SendStringToPrinter(PrinterName, ProcessedText & vbNewLine)
                Catch ex As Exception
                    MsgBox(ex.Message)
                End Try
            End If
            'Add the content to the RichTextBox
            RichTextBox1.AppendText(ProcessedText)
                RichTextBox1.SelectionStart = Len(RichTextBox1.Text)
                RichTextBox1.ScrollToCaret()
                TextBox1.Text = ""
            End If

        'NOT WORKING (replace Enter / Newline when pasting from clippboard)

        'If e.Modifiers = Keys.Control AndAlso e.KeyCode = Keys.V Then
        'e.Handled = True
        'Dim CBTemp As String = Clipboard.GetText.Replace(vbCrLf, " ")
        'TextBox1.Text = ""
        'TextBox1.Text = CBTemp
        'End If

    End Sub

    Private Function ProcessText(ByVal INPUT As String)
        'This Function takes the input text and processes it before adding it to the textbox.
        'This can happen in either smart lines mode (Default) or direct with hard capped lines.
        Dim TEXTPROCOUTPUT As String = ""
        If INPUT = "" Or INPUT.Replace(" ", "") = "" Then
            TEXTPROCOUTPUT = vbNewLine
        Else
            If CheckBox3.Checked = True Then
                'Smart lines mode checks if the next can still fit the current lines character limit and if not, put's it onto the next line insted.
                If RichTextBox1.Text = "" = False Then RichTextBox1.AppendText(vbNewLine)

                Dim CharLoopTemp As String = ""
                Dim CharLoopCount As Integer = 0

                Dim CharWordLoopTemp As String = ""
                Dim CharWordLoopFinal As String = ""
                Dim blockinit As Integer = 0
                Dim blockspace As Integer = 0
                Dim FirstWordFlag As Integer = 0
                For Each s As String In TextBox1.Text.Split(" ")
                    s = LetterReplace(s)
                    For Each c As Char In s
                        If CharLoopCount >= ComboBox1.SelectedItem Then
                            If CharWordLoopTemp = "" Then
                                CharWordLoopTemp = CharLoopTemp
                                FirstWordFlag = 1
                            Else
                                CharWordLoopTemp = CharWordLoopTemp & vbNewLine & CharLoopTemp
                            End If
                            blockspace = 1
                            CharLoopTemp = ""
                            CharLoopCount = 0
                        End If
                        CharLoopTemp = CharLoopTemp & c
                        CharLoopCount = CharLoopCount + 1
                    Next
                    If CharLoopCount <= 0 Then
                        CharWordLoopFinal = CharWordLoopFinal & vbNewLine
                    End If
                    If FirstWordFlag = 1 Then CharWordLoopTemp = CharWordLoopTemp & vbNewLine
                    s = CharLoopTemp
                    FirstWordFlag = 0
                    CharLoopTemp = ""
                    CharLoopCount = 0
                    If CharWordLoopTemp.Length + s.Length >= ComboBox1.SelectedItem - 1 Then
                        If blockinit = 0 Then
                            CharWordLoopFinal = CharWordLoopFinal & CharWordLoopTemp
                            blockinit = 1
                        Else
                            CharWordLoopFinal = CharWordLoopFinal & CharWordLoopTemp & vbNewLine
                        End If
                        CharWordLoopTemp = s
                    Else
                        If blockinit = 0 Then
                            CharWordLoopTemp = CharWordLoopTemp & s
                            blockinit = 1
                        Else
                            CharWordLoopTemp = CharWordLoopTemp & " " & s
                        End If
                    End If
                Next
                TEXTPROCOUTPUT = CharWordLoopFinal & CharWordLoopTemp
            Else
                'Direct mode makes a new line past the character per line limit, regardless of weather or not it chops up words in the middle
                If RichTextBox1.Text IsNot "" Then TEXTPROCOUTPUT = vbNewLine
                Dim CharLoopTemp As String = ""
                Dim CharLoopCount As Integer = 0
                Dim blockspace As Integer = 0
                For Each c As Char In TextBox1.Text
                    If CharLoopCount >= ComboBox1.SelectedItem Then
                        If blockspace = 1 Then TEXTPROCOUTPUT = TEXTPROCOUTPUT & vbNewLine
                        TEXTPROCOUTPUT = TEXTPROCOUTPUT & CharLoopTemp
                        CharLoopTemp = ""
                        CharLoopCount = 0
                        blockspace = 1
                    End If
                    CharLoopTemp = CharLoopTemp & c
                    CharLoopCount = CharLoopCount + 1
                Next
                If CharLoopCount > 0 & vbNewLine And blockspace = 1 Then
                    TEXTPROCOUTPUT = TEXTPROCOUTPUT & vbNewLine & CharLoopTemp
                Else
                    TEXTPROCOUTPUT = TEXTPROCOUTPUT & CharLoopTemp
                End If
            End If

            'Return the formatted string.
        End If
        Return TEXTPROCOUTPUT

    End Function


    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        'Toggle between normal or direct mode.
        If CheckBox1.Checked = True Then
            Button1.Text = "Eject"
        Else
            Button1.Text = "Print"
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'This let's the user set which pinter is used.
        If PrintDialog1.ShowDialog = Windows.Forms.DialogResult.Cancel Then
            Exit Sub
        End If
        TextBox2.Text = PrintDialog1.PrinterSettings.PrinterName
        PrinterName = TextBox2.Text
        My.Settings.PrinterName = TextBox2.Text
        My.Settings.Save()
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        'Lock or unlock RichTextBox1 so the user can edit it.
        If CheckBox2.Checked = True Then
            RichTextBox1.ReadOnly = True
        Else
            RichTextBox1.ReadOnly = False
        End If
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        'changes and saves the charaters per line limit which the user set's via the ComboBox
        My.Settings.CharLimit = ComboBox1.SelectedIndex
        My.Settings.Save()
    End Sub

    Private Function LetterReplace(ByVal TextIN As String)
        'This function replaces certain characters with alternatives as they are not suppored by the printer.
        TextIN = TextIN.Replace("Ü", "Ue")
        TextIN = TextIN.Replace("ü", "ue")
        TextIN = TextIN.Replace("Ä", "Ae")
        TextIN = TextIN.Replace("ä", "ae")
        TextIN = TextIN.Replace("Ö", "Oe")
        TextIN = TextIN.Replace("ö", "oe")
        TextIN = TextIN.Replace("ß", "ss")
        Return TextIN
    End Function

    Private Sub CheckBox3_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox3.CheckedChanged
        'toggles between SmartLines mode and Direct Mode and saves the settings.
        If CheckBox3.Checked = True Then
            My.Settings.LineMode = 1
        Else
            My.Settings.LineMode = 0
        End If
        My.Settings.Save()
    End Sub
End Class