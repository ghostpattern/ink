using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ink;
using Ink.Parsed;
using Ink.Runtime;
using Choice = Ink.Parsed.Choice;
using Path = System.IO.Path;
using Story = Ink.Parsed.Story;
using Tag = Ink.Parsed.Tag;

namespace InkPlugin
{
    internal class ExtraDataOutputPlugin : Ink.IPlugin
    {
        public ExtraDataOutputPlugin ()
        {
        }

        public void PostParse(Story parsedStory, CommandLineTool.Options opts)
        {
            var choiceTextList = new List<Text>();
            var lineTextList = new List<Text>();

            var choiceJsonList = new List<object>();
            var lineJsonList = new List<object>();
            var knotJsonList = new List<object>();
            
            var allChoices = parsedStory.FindAll<Choice>();
            foreach(Choice choice in allChoices)
            {
                if(choice.startContent != null)
                {
                    Text firstText = choice.startContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                        firstText.text = ProcessLineKey(firstText.text, "%^CHOICE&LINE%^");
                    }
                }
                else if(choice.choiceOnlyContent != null)
                {
                    Text firstText = choice.choiceOnlyContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        firstText.text = ProcessLineKey(firstText.text, "%^CHOICE%^");
                    }
                }
                else if(choice.innerContent != null)
                {
                    Text firstText = choice.innerContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                        firstText.text = ProcessLineKey(firstText.text, "%^CHOICE&LINE%^");
                    }
                }
            }

            var allText = parsedStory.FindAll<Text>();
            foreach(Text text in allText)
            {
                if(text.text != null && text.text.Equals("\n") == false)
                {
                    if(text.text.StartsWith("%^") == false)
                    {
                        lineTextList.Add(text);
                        text.text = ProcessLineKey(text.text, "%^LINE%^");
                    }
                }
            }

            foreach(Text choiceText in choiceTextList)
            {
                choiceJsonList.Add(choiceText.text.Substring(choiceText.text.LastIndexOf("%^", StringComparison.Ordinal) + 2));
            }

            foreach(Text lineText in lineTextList)
            {
                lineJsonList.Add(lineText.text.Substring(lineText.text.LastIndexOf("%^", StringComparison.Ordinal) + 2));
            }

            var allKnots = parsedStory.FindAll<Knot>();
            foreach(var knot in allKnots)
            {
                if(knot.debugMetadata.fileName.Equals(opts.inputFile))
                    knotJsonList.Add(knot.name);
            }

            Dictionary<string, object> dataDictionary = new Dictionary<string, object>
            {
                {"ChoiceList", choiceJsonList},
                {"LineList", lineJsonList},
                {"KnotList", knotJsonList}
            };

            var jsonString = SimpleJson.DictionaryToText(dataDictionary);

            string outputFile = opts.outputFile.Replace(".json", "_extradata.json");

            File.WriteAllText(outputFile, jsonString, System.Text.Encoding.UTF8);
        }

        public void PostExport(Ink.Parsed.Story parsedStory, Ink.Runtime.Story runtimeStory)
        {
        }

        private string ProcessLineKey(string text, string type)
        {
            text = text.Insert(0, type);

            return text;
        }
    }
}