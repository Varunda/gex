# gex.WebhookProxy

this project is a small proxy that takes in requests to `/proxy` and sends them to the `Target` query parameter

the server also requires a `ProxySecret` to be sent as a header.
this acts as a shared secret between Gex and the proxy, preventing anyone from calling this endpoint and sending out webhooks

### setup

1. copy `secret.template.json` to `secret.json` and fill in a shared secret key. Gex will also need to be configured to use this same secret
1. `dotnet build`
1. `dotnet run`
