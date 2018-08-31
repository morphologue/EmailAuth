# EmailAuth
A containerised helper service for Nginx email proxying which defers to IdentityWs

## External documentation
* [Nginx authentication protocol](https://nginx.org/en/docs/mail/ngx_mail_auth_http_module.html?&_ga=2.216405534.969938133.1535757971-649503206.1535098745#protocol)
* [IdentityWs](https://github.com/morphologue/IdentityWs)

## Configuration
The `appsettings.json` file should contain a `ProxyDestinations` section with an array of objects like the following:

```json
{
    "Protocol": "MAIL_PROTOCOL",
    "Authenticated": false,
    "Host": "MAIL_SERVER",
    "Port": 100
}
```

`"Authenticated": false` stands for the `none` authentication method; `true` for the `plain` authentication method. Other authentication methods are not supported.

## Building a Docker container
From the top level of the checkout:
```
./publish.sh
sudo docker build -t wedding .
```

