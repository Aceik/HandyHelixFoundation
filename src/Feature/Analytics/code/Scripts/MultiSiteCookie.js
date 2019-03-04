/// Instructions
/// 1) Update the list of known domains in \App_Config\Include\Project\XDB\Site.Common.Website.Analytics.MultiSite.Settings.config  for allowed domains
/// 2) Ensure that a page called /MultiSiteCooke is installed on the main domain. The page should use the Layout @ \views\Layouts\AnalyticsCookieLayout.cshtml.
/// 3) The setting in the file from Step 1, needs to be set to the main domain. Setting Name: Site.Common.Website.MainDomain
/// 4) Must Insert a div with ID: #multi-site-tracker in the main layout HTML for the placeholder
/// 5) Insert the GTM Snippet from /Scripts/GTM-Snippet.js
/// 6) Add this javascript file to the page bundle. Its safe to load this JS Async.

var MultiSiteCookie = {
    isRunning: false,
    settings: {
        iframeDivPlaceholderId: "#multi-site-tracker"
        globalCookieName: $("body").attr("data-global-cookie-name"),
        localCookieName: $(MultiSiteCookie.settings.iframeDivPlaceholderId).attr("data-local-cookie-name"),
        safeDomains: [$("body").attr("data-safe-domains")]
    }, bootup: function () {

        var attr = $("body").attr("data-global-cookie-name");
        if (typeof attr !== typeof undefined && attr !== false) { // If we are in the Iframe
            MultiSiteCookie.iframeInternalFunctions.init();
        } else if ($(MultiSiteCookie.settings.iframeDivPlaceholderId).length > 0) { // If we are in the parent
            MultiSiteCookie.localWebsiteFunctions.init();
        }

        MultiSiteCookie.isRunning = true;
    },
    iframeInternalFunctions: {
        init: function () {
            MultiSiteCookie.iframeInternalFunctions.addMessageListener();
            MultiSiteCookie.iframeInternalFunctions.sendMessage("bootupComplete");
        },
        addMessageListener: function () {
            window.addEventListener('message', MultiSiteCookie.receiveMessage);
        },
        receiveMessage: function (e) {
            // Check to make sure that this message came from the correct domain.
            var originDomain = e.origin;
            if (originDomain.indexOf("http") > -1)
                originDomain = originDomain.replace(location.protocol + "//", "");
            if (!(MultiSiteCookie.settings.safeDomains.indexOf(originDomain) > -1)) {
                console.log("request from an unsafe site.")
                return;
            }

            var storedCid = MultiSiteCookie.data.getCookie(MultiSiteCookie.settings.cookieName);
            if (typeof storedCid == "undefined") {
                MultiSiteCookie.data.setCookie(MultiSiteCookie.settings.cookieName, e.data);
                MultiSiteCookie.iframeInternalFunctions.sendMessage("cid-" + e.data);
            } else {
                MultiSiteCookie.iframeInternalFunctions.sendMessage("cid-" + storedCid);
            }
        },
        sendMessage: function (message) {
            parent.postMessage(message, "*");
        }
    },
    localWebsiteFunctions: {

        /**
         *  This function is called only if the local cookie is not already set.
         *  This ensures this routine of reading cookie data from the IFRAME maindomain.com.au/MultiSiteCooke is run only once per user.
         *  This function is delayed by 5 seconds so that Page Load speed is not impacted by loading an additional IFRAME.
         */
        init: function () {
            if (typeof MultiSiteCookie.data.getCookie(MultiSiteCookie.settings.localCookieName) === "undefined") {
                window.setTimeout(function () {
                    MultiSiteCookie.localWebsiteFunctions.init();
                }, 5000);
            } else {
                $(MultiSiteCookie.settings.iframeDivPlaceholderId).html("<iframe id='receiver' src='//" + $(MultiSiteCookie.settings.iframeDivPlaceholderId).data("url") + "/MultiSiteCooke' width='0' height='0'>");
                MultiSiteCookie.localWebsiteFunctions.recieveBroadcast();
            }
        },
        /**
         * Allows the Client ID to updated in the C# code so that XDB can identify all session with the same Google Client ID.
         * This prevents duplicates in XDB and allows for better user tracking.
         * Only triggers if the cookie is not already present.
         * @param {*} cookieName 
         * @param {*} shouldUpdate 
         * @param {*} cidOverride 
         */
        associateClientId: function (cookieName, shouldUpdate, cidOverride) {
            if (typeof MultiSiteCookie.data.getCookie(cookieName) === "undefined") {
                window.dataLayer = window.dataLayer || [];
                var cid = "";
                if (typeof cidOverride !== "undefined") {
                    cid = cidOverride;
                } else if (typeof ga !== "undefined") {
                    cid = ga.getAll()[0].get("clientId");
                }

                if (typeof cid !== "undefined") {
                    var setEventPath = '/api/xdb/Analytics/TriggerEvent/Event/?eventName=updateGoogleCid&data=' + cid;
                    $.ajax({
                        type: 'POST',
                        url: setEventPath,
                        dataType: 'json',
                        success: function (json) {
                            MultiSiteCookie.data.setCookie(cookieName, cid, 30);
                        },
                        error: function () {
                            console.warn("An error occurred triggering the ticket accesso goal for XDB");
                        }
                    });
                }
            }
        }, 
        /**
         * This function sends the current CID to the IFRAME hosted at maindomain.com.au/MultiSiteCooke.
         * Messages are sent back and recieved via the recieveBroadcast() function.
         * @param {*} cid 
         * @param {*} contentWindow 
         */
        sendBroadcast: function (cid, contentWindow) {
            if (cid != "")
                contentWindow.postMessage(cid, (location.protocol + "//" + $(MultiSiteCookie.settings.iframeDivPlaceholderId).data("url")));
        }, 
        /**
         *  This method is used to listen for broadchast message from the child IFRAME run inside of the maindomain.com.au website.
         *  1) The first message expected is "bootupComplete" - This tells us the IFRAME has loaded and is ready to respond.
         *  2) The second message expected is "cid-123123..."  - This contains the first Client ID value that a visitor was associated with across any of the safe websites.
         * 
         */
        recieveBroadcast: function () {
            window.addEventListener('message', function (event) {
                if (event.origin !== (location.protocol + "//" + $(MultiSiteCookie.settings.iframeDivPlaceholderId).data("url")))
                    return;

                if (event.data === "bootupComplete") {
                    MultiSiteCookie.localWebsiteFunctions.initialiseBroadcast();
                } else if (event.data && event.data.indexOf("cid-") > -1) {
                    var cidNormalised = event.data.replace("cid-", "");

                    // Send the CID obtained from the common storage into the cookie.
                    // GTM will use this on the next page load to set the ClientID  when ga.create is called.
                    MultiSiteCookie.localWebsiteFunctions.associateClientId(MultiSiteCookie.settings.localCookieName, false, cidNormalised);

                    try {
                        // If the CID from the common cookie does not match current stored cookie, attempt to change it
                        // This only helps if its the first time the user has landed on one of the websites. 
                        // In this case the CID may not have been read from the common cookie in the IFrame yet. 
                        // Any subsequent visit and the cookie is already in place and the snippet in GTM reads the value directly.
                        if (typeof ga !== "undefined") {
                            newCid = ga.getAll()[0].get("clientId");
                            if (newCid != cidNormalised)
                                ga.getAll()[0].b.data.values[":clientId"] = cidNormalised;
                        }
                    }
                    catch (err) {
                        if (typeof console !== "undefined")
                            console.log(err)
                    }
                }
            }, false);
        },
        /**
         *   Call this method after the IFRAME on themeparks.com.au/CookiePage  has been injected.
         *   The method checks to see if the contentWindow is ready to go and if it is passes control to initialiseBroadcast().
         */
        initialiseBroadcast: function () {
            var cid = "";
            if (typeof ga !== "undefined") {
                cid = ga.getAll()[0].get("clientId");
            }

            // Make sure the Iframe is ready
            window.setTimeout(function () {
                var recieverWindow = $("#group-sites > #receiver")[0];
                if (typeof recieverWindow === "undefined" || typeof recieverWindow.contentWindow === "undefined") {
                    MultiSiteCookie.localWebsiteFunctions.initialiseBroadcast($("#group-sites").data("url"))
                } else {
                    MultiSiteCookie.localWebsiteFunctions.sendBroadcast(cid, recieverWindow.contentWindow);
                }
            }, 1000);
        }
    },
    data: {
        getCookie: function (name) {
            var value = "; " + document.cookie;
            var parts = value.split("; " + name + "=");
            if (parts.length == 2) return parts.pop().split(";").shift();
        }, setCookie: function (cname, cvalue, exdays) {
            var d = new Date();
            d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
            var expires = "expires=" + d.toUTCString();
            document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
        }
    }
};

(function () {
    MultiSiteCookie.bootup();
})();