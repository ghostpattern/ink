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
                    }
                }
                else if(choice.choiceOnlyContent != null)
                {
                    Text firstText = choice.choiceOnlyContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                    }
                }
                else if(choice.innerContent != null)
                {
                    Text firstText = choice.innerContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                    }
                }
            }

            var allText = parsedStory.FindAll<Text>();
            foreach(Text text in allText)
            {
                if(text.text != null && text.text.Equals("\n") == false)
                {
                    if(choiceTextList.Contains(text) == false && lineTextList.Contains(text) == false)
                    {
                        lineTextList.Add(text);
                    }
                }
            }

            foreach(Text choiceText in choiceTextList)
            {
                choiceJsonList.Add(choiceText.text);
            }

            foreach(Text lineText in lineTextList)
            {
                lineJsonList.Add(lineText.text);
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
    }
}