IIS Lets Encrypt Installer
==========================

IIS-LetsEncrypt-Installer is single certificate installer for IIS with multiple subject alternative names for each website and it's bindings. This is applicable only for very small (less then 100) hosts/websites combination on single IIS server.

Ideal for development/testing at the moment .... because we don't consider lets encrypt to be production ready as when you run out of your limits, you are blocked and your business can suffer serious down time. We appreciate letsencrypt's mission of automation, but in today's world with continous attacks, too much automation with a week worth of blocking will certainly harm large production workloads.

Features
--------

1. Automatically create single certificate with multiple subject alternative names and bind every website with single certificate
2. Delete old bindings

Features Pending
----------------

1. Delete old certificates
2. Renew
