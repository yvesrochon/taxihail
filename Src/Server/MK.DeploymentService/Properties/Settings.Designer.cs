﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34011
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MK.DeploymentService.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Data\\")]
        public string DeployFolder {
            get {
                return ((string)(this["DeployFolder"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Data\\TaxiHailPackages\\")]
        public string DropFolder {
            get {
                return ((string)(this["DropFolder"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Dev")]
        public string ServerName {
            get {
                return ((string)(this["ServerName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://customer.taxihail.com/api/")]
        public string CustomerPortalUrl {
            get {
                return ((string)(this["CustomerPortalUrl"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=CL-T077-082CL;Failover Partner=CL-T078-073CN;Initial Catalog={0};Inte" +
            "grated Security=False;User ID=taxihaildbuser;Password=Mkc1234!;MultipleActiveRes" +
            "ultSets=True")]
        public string SqlConnectionString {
            get {
                return ((string)(this["SqlConnectionString"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=.;Initial Catalog={0};Integrated Security=True; MultipleActiveResultS" +
            "ets=True")]
        public string SqlConnectionStringAllLocal {
            get {
                return ((string)(this["SqlConnectionStringAllLocal"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=CL-T077-082CL;Failover Partner=CL-T078-073CN;Initial Catalog={0};Inte" +
            "grated Security=False;User ID=taxihaildbuser;Password=Mkc1234!;MultipleActiveRes" +
            "ultSets=True")]
        public string SqlConnectionStringDbServer {
            get {
                return ((string)(this["SqlConnectionStringDbServer"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Default Web Site")]
        public string SiteName {
            get {
                return ((string)(this["SiteName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ReplicationEnabled {
            get {
                return ((bool)(this["ReplicationEnabled"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("CL-T078-073CN")]
        public string ReplicatedServer {
            get {
                return ((string)(this["ReplicatedServer"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=CL-T077-082CL;Initial Catalog={0};Integrated Security=True; MultipleA" +
            "ctiveResultSets=True")]
        public string SqlConnectionStringMaster {
            get {
                return ((string)(this["SqlConnectionStringMaster"]));
            }
        }
    }
}
