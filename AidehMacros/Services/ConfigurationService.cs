using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AidehMacros.Models;
using Newtonsoft.Json;

namespace AidehMacros.Services
{
    public class ConfigurationService
    {
        private readonly string _configPath;
        private readonly string _configDirectory;
        
        public ConfigurationService()
        {
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AidehMacros");
            _configPath = Path.Combine(_configDirectory, "config.json");
            
            // Ensure config directory exists
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }
        
        public Configuration LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    return new Configuration();
                }
                
                var json = File.ReadAllText(_configPath);
                var config = JsonConvert.DeserializeObject<Configuration>(json) ?? new Configuration();
                
                // Link actions to mappings for easier access
                foreach (var mapping in config.Mappings)
                {
                    mapping.Action = config.Actions.FirstOrDefault(a => a.Id == mapping.ActionId);
                }
                
                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                return new Configuration();
            }
        }
        
        public void SaveConfiguration(Configuration configuration)
        {
            try
            {
                var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
        
        public void AddOrUpdateAction(Configuration config, MacroAction action)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateAction: Adding/updating action '{action.Name}' with ID '{action.Id}'");
            
            var existing = config.Actions.FirstOrDefault(a => a.Id == action.Id);
            if (existing != null)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateAction: Updating existing action");
                var index = config.Actions.IndexOf(existing);
                config.Actions[index] = action;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateAction: Adding new action");
                config.Actions.Add(action);
            }
            
            SaveConfiguration(config);
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateAction: Action saved. Total actions: {config.Actions.Count}");
        }
        
        public void RemoveAction(Configuration config, string actionId)
        {
            var action = config.Actions.FirstOrDefault(a => a.Id == actionId);
            if (action != null)
            {
                config.Actions.Remove(action);
                
                // Remove any mappings that use this action
                var mappingsToRemove = config.Mappings.Where(m => m.ActionId == actionId).ToList();
                foreach (var mapping in mappingsToRemove)
                {
                    config.Mappings.Remove(mapping);
                }
                
                SaveConfiguration(config);
            }
        }
        
        public void AddOrUpdateMapping(Configuration config, MacroMapping mapping)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateMapping: Adding/updating mapping for key '{mapping.TriggerKey}' with ActionId '{mapping.ActionId}'");
            
            var existing = config.Mappings.FirstOrDefault(m => m.Id == mapping.Id);
            if (existing != null)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateMapping: Updating existing mapping");
                var index = config.Mappings.IndexOf(existing);
                config.Mappings[index] = mapping;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateMapping: Adding new mapping");
                config.Mappings.Add(mapping);
            }
            
            // Update the action reference
            var action = config.Actions.FirstOrDefault(a => a.Id == mapping.ActionId);
            mapping.Action = action;
            
            if (action != null)
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateMapping: Linked action '{action.Name}' to mapping");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateMapping: WARNING - No action found with ID '{mapping.ActionId}'");
            }
            
            SaveConfiguration(config);
            System.Diagnostics.Debug.WriteLine($"ConfigurationService.AddOrUpdateMapping: Configuration saved");
        }
        
        public void RemoveMapping(Configuration config, string mappingId)
        {
            var mapping = config.Mappings.FirstOrDefault(m => m.Id == mappingId);
            if (mapping != null)
            {
                config.Mappings.Remove(mapping);
                SaveConfiguration(config);
            }
        }
        
        public void SetMacroKeyboard(Configuration config, string keyboardDeviceId)
        {
            config.MacroKeyboardDeviceId = keyboardDeviceId;
            SaveConfiguration(config);
        }
    }
    
    public class Configuration
    {
        public string MacroKeyboardDeviceId { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<MacroAction> Actions { get; set; } = new List<MacroAction>();
        public List<MacroMapping> Mappings { get; set; } = new List<MacroMapping>();
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        public MacroMapping? GetMappingForKey(string triggerKey)
        {
            return Mappings.FirstOrDefault(m => 
                m.IsEnabled && 
                m.TriggerKey.Equals(triggerKey, StringComparison.OrdinalIgnoreCase) &&
                m.KeyboardDeviceId == MacroKeyboardDeviceId);
        }
    }
} 