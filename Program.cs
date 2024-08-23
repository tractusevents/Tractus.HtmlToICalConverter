using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Globalization;

namespace Tractus.HtmlToICalConverter;

internal class Program
{
    static void Main(string[] args)
    {
        if(args.Any(x=> x == "-html"))
        {
            return;
        }
        else if(args.Any(x=>x == "-md"))
        {

        }
        else if(args.Any(x=>x == "-mjml"))
        {
            return;
        }
        else
        {
            Console.WriteLine("You must specify one of -html, -md, or -mjml (only -md supported for now).");
            return;
        }

        var fileName = args.FirstOrDefault(x => x.StartsWith("-f="))?.Split("-f=", StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();
        if (string.IsNullOrEmpty(fileName))
        {
            Console.WriteLine("No file name specified with -f");
            return;
        }

        var fileContents = File.ReadAllText(fileName);

        var pipeline = new MarkdownPipelineBuilder().Build();
        var textWriter = new StringWriter();

        var customRenderer = new HtmlRenderer(textWriter)
        {
            EnableHtmlEscape = false,
            EnableHtmlForBlock = false,
            EnableHtmlForInline = false,
        };
        
        var document = Markdown.Parse(fileContents, pipeline);
        var html = Markdown.ToHtml(fileContents);

        // This will convert <a> links to their URL equivalent for the plaintext ICS.
        foreach(LinkInline link in document.Descendants<LinkInline>())
        {
            var child = link.LastChild as LiteralInline;

            if (child != null)
            {
                child.Content = new Markdig.Helpers.StringSlice(link.Url);
            }
        }

        pipeline.Setup(customRenderer);
        customRenderer.Render(document);
        textWriter.Flush();

        var plainText = textWriter.ToString();

        var subject = args.FirstOrDefault(x => x.StartsWith("-s="))?.Split("-s=")?.LastOrDefault();
        if (string.IsNullOrEmpty(subject))
        {
            Console.WriteLine("No subject specified with -s");
            return;
        }

        var dateOfEvent = args.FirstOrDefault(x => x.StartsWith("-d="))?.Split("-d=")?.LastOrDefault();
        if (string.IsNullOrEmpty(dateOfEvent))
        {
            Console.WriteLine("No date specified with -d");
            return;
        }

        var timeOfEventParsed = DateTime.ParseExact(dateOfEvent, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var timeOfEventLocal = DateTime.SpecifyKind(timeOfEventParsed, DateTimeKind.Local);

        var duration = args.FirstOrDefault(x => x.StartsWith("-l="))?.Split("-l=")?.LastOrDefault();
        if (string.IsNullOrEmpty(duration))
        {
            Console.WriteLine("No duration in minutes specified with -l");
            return;
        }
        var durationMinutes = int.Parse(duration);

        var reminder = args.FirstOrDefault(x => x.StartsWith("-alarm="))?.Split("-alarm=")?.LastOrDefault();
        if (string.IsNullOrEmpty(reminder))
        {
            reminder = "0";
        }

        var reminderMinutes = int.Parse(reminder);


        var location = args.FirstOrDefault(x => x.StartsWith("-w="))?.Split("-w=")?.LastOrDefault();
        if (string.IsNullOrEmpty(location))
        {
            Console.WriteLine("No location (where) specified with -w");
            return;
        }

        var outputFileName = args.FirstOrDefault(x => x.StartsWith("-o="))?.Split("-o=")?.LastOrDefault();

        var toReturn = new HtmlToICalConverter().Convert(
            subject, 
            timeOfEventLocal.ToUniversalTime(), 
            durationMinutes, 
            location, 
            html, 
            plainText,
            reminderMinutes);

        var toWrite = new CalendarSerializer().SerializeToString(toReturn);

        toWrite = toWrite.Replace(
            "X-ALT-DESC:FMTTYPE=text/html",
            "X-ALT-DESC;FMTTYPE=text/html");

        if (string.IsNullOrEmpty(outputFileName))
        {
            Console.Write(toWrite);
        }
        else
        {
            File.WriteAllText(outputFileName, toWrite);
        }
    }
}

public class HtmlToICalConverter
{
    public Ical.Net.Calendar Convert(
        string subject,
        DateTime startDateUtc,
        int minutes,
        string location,
        string htmlContent,
        string plainTextContent,
        int reminderMinutesBefore)
    {
        var calendar = new Ical.Net.Calendar();

        var e = calendar.Create<CalendarEvent>();

        e.DtStart = new Ical.Net.DataTypes.CalDateTime(startDateUtc, "UTC");
        e.Duration = TimeSpan.FromMinutes(minutes);

        e.Summary = subject;
        e.Location = location;

        e.Alarms.Add(new Alarm()
        {
            Action = AlarmAction.Audio,
            Trigger = new Ical.Net.DataTypes.Trigger(TimeSpan.FromMinutes(reminderMinutesBefore)),
            Description = subject
        });

        //if (!string.IsNullOrEmpty(fullRequest.Event.EventUid))
        //{
        //    e.Class = "PUBLIC";
        //    e.Uid = fullRequest.Event.EventUid;
        //    e.Status = fullRequest.Event.Status;
        //    e.Sequence = int.Parse(fullRequest.Event.Sequence);
        //    e.Organizer = new Ical.Net.DataTypes.Organizer(replyToAddress);

        //    e.Attendees = new List<Ical.Net.DataTypes.Attendee>()
        //            {
        //                new Ical.Net.DataTypes.Attendee()
        //                {
        //                    Value = new Uri($"mailto:{recipientEmail}"),
        //                    ParticipationStatus = "REQ-PARTICIPANT",
        //                    Rsvp = false,
        //                }
        //            };

        //    calendar.Method = fullRequest.Event.Method ?? "REQUEST";
        //}

        e.Description = plainTextContent;

        e.AddProperty(new Ical.Net.CalendarProperty(
            "X-ALT-DESC",
            $"FMTTYPE=text/html:{htmlContent}"));

        return calendar;
    }
}
