# Tractus Markdown to .ics Converter

Creates an ICS from a Markdown file.

### Command Line Arguments

`-md`: Required - specifies that a markdown file is the input.
`-s`: Subject line/description.
`-f`: The source file to convert
`-d`: The date/time in YOUR LOCAL TIME ZONE. yyyy-MM-dd HH:mm
`-l`: The length of the event in whole minutes
`-w`: The location field.
`-alarm`: Minutes before start when the meeting reminder should be displayed.
`-o`: The output file name.

### Example Usage

`.\Tractus.HtmlToICalConverter.exe -md -s="Test Event" -f="test.md" -d="2024-08-24 12:00" -l=55 -w="Zoom - See Details" -alarm=45 -o="testevent.ics"`

