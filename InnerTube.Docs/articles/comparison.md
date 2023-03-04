# Comparison to the YouTube Data v3 API

The official API requires a Google account to use, while the InnerTube API can do it without the need of one, although
with some limited functionality (age-gated videos).

The official API has a request quota and a request rate limit, while the InnerTube API does not have any limitations.

The official API is designed to be used by 3rd parties, thus, the data is formatted well. Since the InnerTube API is
made to be used in the official YouTube apps, it is made to render elements on a screen. This makes it slightly harder
to parse the output to a human readable format, which this library tries to do.
