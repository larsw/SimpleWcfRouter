namespace SimpleWcfRouter
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;

    [ServiceContract]
    public interface IDuck
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message Process(Message request);
    }

    public interface IDuckClient : IDuck, IClientChannel
    {
    }

    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class Duck : IDuck
    {
        public Message Process(Message request)
        {

            IDuckClient client = null;
            var binding = new BasicHttpBinding();
            if (request.Headers.Action.Contains("v2"))
            {
                client = ChannelFactory<IDuckClient>.CreateChannel(binding,
                                                             new EndpointAddress("http://localhost/v2/Foo.svc"));
            }
            else
            {
                client = ChannelFactory<IDuckClient>.CreateChannel(binding,
                                                             new EndpointAddress("http://localhost:12345/a"));
            }
            return client.Process(request);
        }
    }

    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        string Op(string message);
    }

    public interface IServiceClient : IService, IClientChannel
    {
    }

    public class Service : IService
    {
        public string Op(string message)
        {
            Console.WriteLine("Op()");
            return message + " foo";
        }
    }

    //
    //
    // It might be that you will need to run
    // netsh http add urlacl url=http://localhost:1235 YOURDOMAIN\username before running this sample.
    // (if running with non-admin privileges.
    //
    class Program
    {
        static void Main()
        {
            var realService = new ServiceHost(typeof (Service), new Uri("http://localhost:12345/a"));
            realService.AddDefaultEndpoints();
            var smb = new ServiceMetadataBehavior {HttpGetEnabled = true};
            realService.Description.Behaviors.Add(smb);
            realService.Open();


            var serviceHost = new ServiceHost(typeof (Duck), new Uri("http://localhost:12345/router"));
            serviceHost.AddDefaultEndpoints();
            serviceHost.Open();
            Console.WriteLine("Service is up and running.");

            var routerClient = ChannelFactory<IServiceClient>.CreateChannel(new BasicHttpBinding(),
                                                                         new EndpointAddress(
                                                                             "http://localhost:12345/router"));
            Console.WriteLine(routerClient.Op("bar"));
            
            Console.ReadLine();
            serviceHost.Close();
            realService.Close();
        }
    }
}
