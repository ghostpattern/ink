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
            var choiceJsonArray = new List<object>();
            var lineJsonArray = new List<object>();
            var knotJsonArray = new List<object>();
            
            var allChoices = parsedStory.FindAll<Choice>();
            foreach(Choice choice in allChoices)
            {
                if(choice.startContent != null)
                {
                    Text firstText = choice.startContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceJsonArray.Add(firstText.text);
                        lineJsonArray.Add(firstText.text);
                    }
                }
                else if(choice.choiceOnlyContent != null)
                {
                    Text firstText = choice.choiceOnlyContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceJsonArray.Add(firstText.text);
                    }
                }
                else if(choice.innerContent != null)
                {
                    Text firstText = choice.innerContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceJsonArray.Add(firstText.text);
                        lineJsonArray.Add(firstText.text);
                    }
                }
            }

            var allText = parsedStory.FindAll<Text>();
            foreach(Text text in allText)
            {
                if(text.text != null && text.text.Equals("\n") == false)
                {
                    lineJsonArray.Add(text.text);
                }
            }

            var allKnots = parsedStory.FindAll<Knot>();
            foreach(var knot in allKnots)
            {
                if(knot.debugMetadata.fileName.Equals(opts.inputFile))
                    knotJsonArray.Add(knot.name);
            }

            Dictionary<string, object> dataDictionary = new Dictionary<string, object>
            {
                {"ChoiceList", choiceJsonArray},
                {"LineList", lineJsonArray},
                {"KnotList", knotJsonArray}
            };


            var jsonString = SimpleJson.DictionaryToText(dataDictionary);

            string outputFile = opts.outputFile.Replace(".json", "_inkdata.json");

            File.WriteAllText(outputFile, jsonString, System.Text.Encoding.UTF8);
        }

        public void PostExport(Ink.Parsed.Story parsedStory, Ink.Runtime.Story runtimeStory)
        {
        }
    }
}