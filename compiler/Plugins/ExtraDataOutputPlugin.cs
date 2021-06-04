using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            if(parsedStory == null)
                return;

            // Get or generate scene key
            string sceneDataFilePath = opts.outputFile.Replace(".json", ".asset");
            int sceneKey = 0;
            if(File.Exists(sceneDataFilePath))
            {
                string sceneData = File.ReadAllText(sceneDataFilePath);
                Match match = Regex.Match(sceneData, @"Key\: ([0-9]*)");
                if(match.Success)
                {
                    if(int.TryParse(match.Groups[1].Value, out sceneKey))
                    {
#if DEBUG
                        Console.WriteLine("found scene key: {0:X16}", sceneKey);
#endif
                    }
                }
            }
            if(sceneKey == 0)
            {
                // wait for unity to generate a scenekey
            }
            string sceneKeyAsString = sceneKey.ToString("X16");
            if(sceneKeyAsString.Length > 4)
            {
                sceneKeyAsString = sceneKeyAsString.Substring(sceneKeyAsString.Length - 4, 4);
            }
            else if(sceneKeyAsString.Length < 4)
            {
                sceneKeyAsString = sceneKeyAsString.PadLeft(4, '0');
            }

            var choiceTextList = new List<Text>();
            var lineTextList = new List<Text>();

            var choiceJsonList = new List<object>();
            var lineJsonList = new List<object>();
            var knotJsonList = new List<object>();
            
            var allChoices = parsedStory.FindAll<Choice>(choice => choice.debugMetadata != null && choice.debugMetadata.fileName == opts.inputFile);
            foreach(Choice choice in allChoices)
            {
                Knot knot = FindKnotParent(choice);

                if(choice.startContent != null)
                {
                    Text firstText = choice.startContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                        firstText.text = ProcessLineKey(firstText.text, sceneKey, knot);
                    }
                }
                else if(choice.choiceOnlyContent != null)
                {
                    Text firstText = choice.choiceOnlyContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        firstText.text = ProcessLineKey(firstText.text, sceneKey, knot);
                    }
                }
                else if(choice.innerContent != null)
                {
                    Text firstText = choice.innerContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                        firstText.text = ProcessLineKey(firstText.text, sceneKey, knot);
                    }
                }
            }

            var allText = parsedStory.FindAll<Text>(text => text.debugMetadata != null && text.debugMetadata.fileName == opts.inputFile);
            foreach(Text text in allText)
            {
                if(text.text != null && text.text.Trim(' ', '\n', '\t').Length > 0)
                {
                    if(text.text.StartsWith("Scene:")
                       || text.text.StartsWith("Trigger:")
                       || text.text.StartsWith("<Trigger:")
                       || text.text.StartsWith("Action:"))
                        continue;

                    Knot knot = FindKnotParent(text);

                    if(text.parent != null && text.parent.typeName == "StringExpression")
                    {
#if DEBUG
                        Console.WriteLine("Skipping {0} because text.parent is StringExpression", text);
#endif
                        continue;
                    }

                    // If this text has already been processed as a choice,
                    // it will already have a key at the start - don't re-process and
                    // add an unnecessary entry.
                    if(text.text.StartsWith(sceneKeyAsString) == false)
                    {
                        lineTextList.Add(text);
                        text.text = ProcessLineKey(text.text, sceneKey, knot);
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

            var allKnots = parsedStory.FindAll<Knot>(knot => knot.debugMetadata != null && knot.debugMetadata.fileName == opts.inputFile);
            foreach(var knot in allKnots)
            {
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

#if DEBUG
            Console.Read();
#endif
        }

        private static Knot FindKnotParent(Ink.Parsed.Object choice)
        {
            Ink.Parsed.Object parent = choice.parent;
            while(parent != null)
            {
                if(parent is Knot)
                    return parent as Knot;

                parent = parent.parent;
            }

            return null;
        }

        public void PostExport(Ink.Parsed.Story parsedStory, Ink.Runtime.Story runtimeStory)
        {

        }

        private string ProcessLineKey(string text, int sceneKey, Knot knot)
        {
            int existingKey;
            if(text.Length > 4 && int.TryParse(text.Substring(0, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out existingKey) && existingKey == sceneKey)
            {
                return text;
            }
#if DEBUG
            Console.WriteLine("ProcessLineKey({0}, {1:X16}, {2})", text, sceneKey, knot != null ? knot.name : "~no knot~");
#endif
            ulong lineHash = (ulong)sceneKey << 48;
#if DEBUG
            Console.WriteLine("lineHash after sceneKey: {0:X16}", lineHash);
#endif

            using(var md5Hasher = MD5.Create())
            {
                if(knot != null)
                {
                    var data1 = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(knot.name));
                    lineHash += (((ulong)BitConverter.ToInt16(data1, 0) % (1 << 16)) << 32);
#if DEBUG
                    Console.WriteLine("lineHash after knot: {0:X16}", lineHash);
#endif

                    var data2 = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(text));
                    lineHash += (uint)BitConverter.ToInt32(data2, 0);
#if DEBUG
                    Console.WriteLine("lineHash after text: {0:X16}", lineHash);
#endif
                }
                else
                {
                    var data1 = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(text));
                    lineHash += (ulong)BitConverter.ToInt64(data1, 0) % ((ulong)1 << 48);
#if DEBUG
                    Console.WriteLine("lineHash after text: {0:X16}", lineHash);
#endif
                }
            }

            text = text.Insert(0, string.Format("{0:X16}", lineHash));

            return text;
        }
    }
}
