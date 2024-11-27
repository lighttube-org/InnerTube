# InnerTube

A wrapper for the private YouTube API used in the YouTube website and the mobile apps.

> [!CAUTION]
> This library is no longer under active development. The 2.0 release is a copy of the final prerelease version, and
> will likely break under use. Use at your own risk.
> 
> https://blog.kuylar.dev/lighttube-end-of-development/

# Comparison to the YouTube Data v3 API

The official API requires a Google account to use, while the InnerTube API can do it without the need of one.

The official API has a request quota and a request rate limit, while the InnerTube API does not have any limitations.

The official API is designed to be used by 3rd parties, thus, the data is formatted well. Since the InnerTube API is
made to be used in the official YouTube apps, it is formatted to render elements on a screen. This makes it slightly
harder to parse the output to a human readable format, which this library tries to do.

# Authentication

There's no straightforward way to authenticate with this library. You will either have to extract cookies from a browser
session, or extract a refresh token. See the wiki page for more details.