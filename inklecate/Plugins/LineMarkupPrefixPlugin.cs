using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ink.Parsed;
using Path = System.IO.Path;

namespace InkPlugin
{
    internal class LineMarkupPrefixPlugin : Ink.IPlugin
    {
        private List<string> _lineMarkupPrefixList; 

        public LineMarkupPrefixPlugin ()
        {
            string pluginDataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "line_markup_prefix_plugin_data.txt");
            if(File.Exists(pluginDataFilePath))
            {
                string pluginDataText = File.ReadAllText(pluginDataFilePath);
                _lineMarkupPrefixList = new List<string>(pluginDataText.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public void PostParse(Ink.Parsed.Story parsedStory)
        {
            if(_lineMarkupPrefixList != null)
            {
                var allChoices = parsedStory.FindAll<Choice>();
                foreach(Choice choice in allChoices)
                {
                    if(choice.startContent != null)
                    {
                        Text firstText = choice.startContent.content[0] as Text;
                        if(firstText != null)
                        {
                            firstText.text = ProcessLineKey(firstText.text, "%^CHOICE&LINE%^");
                        }
                    }
                    if(choice.choiceOnlyContent != null)
                    {
                        Text firstText = choice.choiceOnlyContent.content[0] as Text;
                        if(firstText != null)
                        {
                            firstText.text = ProcessLineKey(firstText.text, "%^CHOICE%^");
                        }
                    }
                    if(choice.innerContent != null)
                    {
                        Text firstText = choice.innerContent.content[0] as Text;
                        if(firstText != null)
                        {
                            firstText.text = ProcessLineKey(firstText.text, "%^CHOICE&LINE%^");
                        }
                    }
                }

                var allText = parsedStory.FindAll<Text>();
                foreach(Text text in allText)
                {
                    if(text.text != null && text.text.StartsWith("%^") == false)
                    {
                        text.text = ProcessLineKey(text.text, "%^LINE%^");
                    }
                }
            }
            else
            {
                // Attach the warning to the first tag we find, so that Unity has a line number and file to attach to.
                parsedStory.Warning(string.Format("LineMarkupPrefixPlugin - couldn't find line_markup_prefix_plugin_data.txt in {0}", Directory.GetCurrentDirectory()), parsedStory.Find<Tag>());
            }
        }

        public void PostExport(Ink.Parsed.Story parsedStory, Ink.Runtime.Story runtimeStory)
        {
        }

        private string ProcessLineKey(string text, string type)
        {
            if(_lineMarkupPrefixList != null && _lineMarkupPrefixList.Find(prefix => text.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)) != null)
            {
                text = text.Insert(0, type);
            }

            return text;
        }

    }
}