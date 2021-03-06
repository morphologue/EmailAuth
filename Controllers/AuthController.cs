﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using EmailAuth.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Morphologue.IdentityWsClient;

namespace EmailAuth.Controllers
{
    public class AuthController : Controller
    {
        private const string VAGUERY = "Invalid login attempt";

        private readonly ILogger<AuthController> _log;
        private readonly string _aliasSuffix;
        private readonly string _clientName;
        private readonly List<ProxyDestination> _proxyDests;
        private readonly IdentityWs _identityWs;

        public AuthController(ILogger<AuthController> log, IConfiguration config, IdentityWs identityWs)
        {
            _log = log;
            _aliasSuffix = config["IdentityWsAliasSuffix"];
            _clientName = config["IdentityWsClientName"];
            _proxyDests = config.GetSection("ProxyDestinations").Get<List<ProxyDestination>>();
            _identityWs = identityWs;
        }

        public async Task<IActionResult> Index()
        {
            switch (Request.Headers["Auth-Method"])
            {
                case "none":
                    return await HandleAnonymousConnectionAsync();
                case "plain":
                    return await HandleIdentifiedConnectionAsync();
                default:
                    _log.LogWarning("Unsupported authentication method {AuthMethod}", Request.Headers["Auth-Method"]);
                    return AuthStatus();
            }
        }

        private async Task<IActionResult> HandleAnonymousConnectionAsync()
        {
            string protocol = Request.Headers["Auth-Protocol"];
            ProxyDestination dest = _proxyDests.FirstOrDefault(d => d.Protocol == protocol && !d.Authenticated);
            if (dest == null)
            {
                _log.LogWarning("Protocol {protocol} not supported for anonymous connections", protocol);
                return AuthStatus();
            }

            Response.Headers["Auth-Server"] = await DnsLookupAsync(dest.Host);
            Response.Headers["Auth-Port"] = dest.Port.ToString();
            return AuthStatus("OK");
        }

        private async Task<IActionResult> HandleIdentifiedConnectionAsync()
        {
            string protocol = Request.Headers["Auth-Protocol"];
            ProxyDestination dest = _proxyDests.FirstOrDefault(d => d.Protocol == protocol && d.Authenticated);
            if (dest == null)
            {
                _log.LogWarning("Protocol {protocol} not supported for identified connections", protocol);
                return AuthStatus();
            }

            string username = Request.Headers["Auth-User"];
            string password = Request.Headers["Auth-Pass"];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _log.LogInformation("Missing credentials", username);
                return AuthStatus();
            }

            string emailAddress = $"{username}{_aliasSuffix}";
            Alias alias = await _identityWs.GetAliasAsync(emailAddress);
            if (alias == null)
            {
                _log.LogInformation("Alias {emailAddress} not found", emailAddress);
                return AuthStatus();
            }

            Client client = await alias.GetClientAsync(_clientName);
            if (client == null)
            {
                _log.LogInformation("Client {CLIENT_NAME} not found in Alias {emailAddress}", _clientName, emailAddress);
                return AuthStatus();
            }

            try
            {
                await client.LogInAsync(password);
            }
            catch (IdentityException ex)
            {
                _log.LogInformation(ex, "Status code {StatusCode} during login", ex.StatusCode);
                return AuthStatus();
            }

            Response.Headers["Auth-Server"] = await DnsLookupAsync(dest.Host);
            Response.Headers["Auth-Port"] = dest.Port.ToString();
            return AuthStatus("OK");
        }

        private IActionResult AuthStatus(string errorMessage = VAGUERY)
        {
            Response.Headers["Auth-Status"] = errorMessage;
            return Ok();
        }

        private async Task<string> DnsLookupAsync(string hostname)
        {
            string result = (await Dns.GetHostAddressesAsync(hostname))
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .FirstOrDefault()
                ?.ToString();

            if (result == null)
            {
                _log.LogError("Could not resolve hostname {hostname}", hostname);
                return hostname;
            }

            return result;
        }
    }
}
