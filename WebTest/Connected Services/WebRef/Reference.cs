﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebTest.WebRef {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://www.aoreestr.ru/", ConfigurationName="WebRef.IReciver", CallbackContract=typeof(WebTest.WebRef.IReciverCallback))]
    public interface IReciver {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IReciver/Register")]
        void Register(System.Guid CompId);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IReciver/Register")]
        System.Threading.Tasks.Task RegisterAsync(System.Guid CompId);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IReciver/UnRegister")]
        void UnRegister(System.Guid CompId);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IReciver/UnRegister")]
        System.Threading.Tasks.Task UnRegisterAsync(System.Guid CompId);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IReciverCallback {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IReciver/Send")]
        void Send(string Text);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.aoreestr.ru/IReciver/Ping", ReplyAction="http://www.aoreestr.ru/IReciver/PingResponse")]
        void Ping();
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IReciverChannel : WebTest.WebRef.IReciver, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ReciverClient : System.ServiceModel.DuplexClientBase<WebTest.WebRef.IReciver>, WebTest.WebRef.IReciver {
        
        public ReciverClient(System.ServiceModel.InstanceContext callbackInstance) : 
                base(callbackInstance) {
        }
        
        public ReciverClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName) : 
                base(callbackInstance, endpointConfigurationName) {
        }
        
        public ReciverClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public ReciverClient(System.ServiceModel.InstanceContext callbackInstance, string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, endpointConfigurationName, remoteAddress) {
        }
        
        public ReciverClient(System.ServiceModel.InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(callbackInstance, binding, remoteAddress) {
        }
        
        public void Register(System.Guid CompId) {
            base.Channel.Register(CompId);
        }
        
        public System.Threading.Tasks.Task RegisterAsync(System.Guid CompId) {
            return base.Channel.RegisterAsync(CompId);
        }
        
        public void UnRegister(System.Guid CompId) {
            base.Channel.UnRegister(CompId);
        }
        
        public System.Threading.Tasks.Task UnRegisterAsync(System.Guid CompId) {
            return base.Channel.UnRegisterAsync(CompId);
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://www.aoreestr.ru/", ConfigurationName="WebRef.IPostman")]
    public interface IPostman {
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IPostman/Post")]
        void Post(System.Guid CompId, string Text);
        
        [System.ServiceModel.OperationContractAttribute(IsOneWay=true, Action="http://www.aoreestr.ru/IPostman/Post")]
        System.Threading.Tasks.Task PostAsync(System.Guid CompId, string Text);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IPostmanChannel : WebTest.WebRef.IPostman, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class PostmanClient : System.ServiceModel.ClientBase<WebTest.WebRef.IPostman>, WebTest.WebRef.IPostman {
        
        public PostmanClient() {
        }
        
        public PostmanClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public PostmanClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public PostmanClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public PostmanClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public void Post(System.Guid CompId, string Text) {
            base.Channel.Post(CompId, Text);
        }
        
        public System.Threading.Tasks.Task PostAsync(System.Guid CompId, string Text) {
            return base.Channel.PostAsync(CompId, Text);
        }
    }
}
