# Using cookies

To authenticate using cookies, you will need to extract 2 cookies from a browser, `__Secure-3PAPISID`
and `__Secure-3PSID`. Both of these are required for the library to authenticate you correctly. After getting these,
just use set the `Authorization` field in your `InnerTubeConfiguration`
to `InnerTubeAuthorization.SapisidAuthorization("YOUR-__Secure-3PAPISID-COOKIE", "YOUR-__Secure-3PSID-COOKIE")`, and it
should work fine! For an
example, [you should check this file](https://github.com/kuylar/InnerTube/blob/master/InnerTube.Tests/AuthenticationTests.cs#L10-L20).

> Note: These cookies can expire at any time. Make sure that you have them in a place where you can update them.

# Using a refresh token

Instead of using cookies that can expire, you can just use a refresh token that works just as well. You can get a
refresh token by doing some debugger magic on the YouTube TV app.

1. Visit https://youtube.com/tv
    * You will need to get a TV user agent to access to the app. If your user agent is invalid, you will see this
      screen:
      ![YouTube TV invalid user agent screen](https://user-images.githubusercontent.com/52961639/200139026-5c471916-fe92-44c4-a068-9ea1a1053675.png)
    * You can bypass this message by using a user agent changer extension, and setting your user agent
      to `Mozilla/5.0 (AppleTV; CPU OS 13_4_6 like Mac OS X) HW_AppleTV/5.3` (Apple TV user agent)
2. Go to the sign in prompt located in the sidebar
   ![YouTube TV sign in prompt](https://user-images.githubusercontent.com/52961639/200139155-a2c317f8-2b79-4f60-9b87-029824912397.png)
    * At this point, you should get your browser's DevTools up and switch to the network
      tab ![Sign in prompt with the DevTools open](https://user-images.githubusercontent.com/52961639/200139220-245ce0b5-6da6-4355-813f-bd32ce9bf916.png)
3. Follow the instructions on the screen, and look for a new request that goes
   to `https://www.youtube.com/o/oauth2/token`. Click on this request, and check its response. Copy the `refresh_token`
   field somewhere, since you will only need that one.  
   ![Response of a successful log in](https://user-images.githubusercontent.com/52961639/200139326-a6803925-e093-4c25-b3cc-8a77b14767b1.png)
4. Put the refresh token you just received inside your `InnerTubeConfiguration`, like so:  
   ![Code example](https://user-images.githubusercontent.com/52961639/200139396-d8f4f06e-c614-4248-b5df-d9d6140cd250.png)

If you did all the steps above correctly, you should now be authorized!