//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace InactiveAssistantMonitor.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.3.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int PeriodIntervalInSeconds {
            get {
                return ((int)(this["PeriodIntervalInSeconds"]));
            }
            set {
                this["PeriodIntervalInSeconds"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int NumberOfIntervalsUntilKill {
            get {
                return ((int)(this["NumberOfIntervalsUntilKill"]));
            }
            set {
                this["NumberOfIntervalsUntilKill"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Program Files (x86)\\UiPath\\Studio\\")]
        public string UiPathAssistantPathX86 {
            get {
                return ((string)(this["UiPathAssistantPathX86"]));
            }
            set {
                this["UiPathAssistantPathX86"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15")]
        public int OffsetChecks {
            get {
                return ((int)(this["OffsetChecks"]));
            }
            set {
                this["OffsetChecks"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int PeriodIntervalConnectionToOrchestrator {
            get {
                return ((int)(this["PeriodIntervalConnectionToOrchestrator"]));
            }
            set {
                this["PeriodIntervalConnectionToOrchestrator"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://staging.uipath.com/benefittrackingdemo/DefaultTenant/orchestrator_/")]
        public string OrchestratorUrl {
            get {
                return ((string)(this["OrchestratorUrl"]));
            }
            set {
                this["OrchestratorUrl"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5b6c6f95-6fac-402f-95bc-346db133887e")]
        public string MachineKey {
            get {
                return ((string)(this["MachineKey"]));
            }
            set {
                this["MachineKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("UiPathAssistant\\UiPath.Assistant.exe")]
        public string UiPathAssistantExe {
            get {
                return ((string)(this["UiPathAssistantExe"]));
            }
            set {
                this["UiPathAssistantExe"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("UiRobot.exe")]
        public string UiPathRobot {
            get {
                return ((string)(this["UiPathRobot"]));
            }
            set {
                this["UiPathRobot"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Program Files\\UiPath\\Studio\\")]
        public string UiPathAssistantPath {
            get {
                return ((string)(this["UiPathAssistantPath"]));
            }
            set {
                this["UiPathAssistantPath"] = value;
            }
        }
    }
}
