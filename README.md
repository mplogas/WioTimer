# Tooth-brushing timer using a WIO Link board

A simple application that controls my [Wio Link](http://wiki.seeed.cc/Wio_Link/). It displays rainbow color for 3 minutes and turns off afterwards.
In order to run the code, get a Wio Link first and attach a Grove button and Grove LED strip. Register your WIO board on your on-premise wio-link server or the provided one. Once you registered your board, you'll get an access key. Put this key into ```WebSocket:Id``` parameter of the config.json.

TODO: decipher the Oral-B Bluetooth communication so it will automatically start when the toothbrush starts. Requires an Oral-B BT-enabled toothbrush and a Grove Serial BT module, obviously.