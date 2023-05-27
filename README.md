# Mock-Server

This project uses WebSockets to mock our customer's APIs so we can test the rest of our project. For example, endpoints will be provided to:

* send AI-detected subtitle data, in a JSON format (likely structured similarly to what GoSpeech offers)
* send the recorded A/V stream, in an unknown format
* receive our A/V + Subtitles stream, in an unknown format
