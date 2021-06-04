using System;
using System.Collections.Generic;
using Ink.Parsed;

namespace Ink
{
    public class PluginManager
    {
        public PluginManager (List<string> pluginNames)
        {
            _plugins = new List<IPlugin> ();

            // TODO: Make these plugin names DLL filenames, and load up their assemblies
            foreach (string pluginName in pluginNames) {
                //if (pluginName == "ChoiceListPlugin") {
                //    _plugins.Add (new InkPlugin.ChoiceListPlugin ());
                if (pluginName == "ExtraDataOutputPlugin") {
                    _plugins.Add (new InkPlugin.ExtraDataOutputPlugin());
                } else {
                    throw new System.Exception ("Plugin not found");
                }
            }
        }

        public void PostParse(Story parsedStory, string inputFile, string outputFile)
        {
            foreach (var plugin in _plugins) {
                plugin.PostParse (parsedStory, inputFile, outputFile);
            }
        }

        public void PostExport(Parsed.Story parsedStory, Runtime.Story runtimeStory)
        {
            foreach (var plugin in _plugins) {
                plugin.PostExport (parsedStory, runtimeStory);
            }
        }

        List<IPlugin> _plugins;
    }
}

