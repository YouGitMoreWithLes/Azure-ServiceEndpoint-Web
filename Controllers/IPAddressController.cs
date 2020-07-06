using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;

namespace Azure_ServiceEndpoint_Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IPAddressController : ControllerBase
    {

        [HttpGet]
        public string[] Get()
        {
            List<string> addresses = new List<string>();

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var network in networkInterfaces)
            {
                var properties = network.GetIPProperties();
                foreach (var address in properties.UnicastAddresses)
                {
                    addresses.Add(address.Address.ToString());
                }
            }

            return addresses.ToArray();
        }
    }
}