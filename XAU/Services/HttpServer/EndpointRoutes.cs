using Newtonsoft.Json;
using System.Net;
using System.Text;

public static class EndpointRoutes
{
    public static Dictionary<string, Func<HttpListenerContext, Task>> GetRoutes()
    {
        return new Dictionary<string, Func<HttpListenerContext, Task>>
        {
            { "/api/", async context => await IndexPageRequest(context) },
            { "/", async context => await IndexPageRequest(context) }
        };
    }

    private static async Task IndexPageRequest(HttpListenerContext context)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        string htmlContent = @"
<!-- meow meow :) -->
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>XAU API Endpoints</title>
    <link rel=""icon"" type=""image/x-icon"" href=""https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/refs/heads/Main/XAU/cirno.ico"">
    <link href=""https://cdnjs.cloudflare.com/ajax/libs/tailwindcss/2.2.19/tailwind.min.css"" rel=""stylesheet"">
    <script>
        function openEndpoint(baseUrl, inputId) {
            const input = document.getElementById(inputId);
            const value = input.value.trim();
            if (value) {
                window.open(baseUrl + encodeURIComponent(value), '_blank');
            } else {
                alert('Oopsie Woopsy Fucky Wucky... Enter a value in the text box.');
            }
        }
        function openEndpointWithTwoInputs(baseUrl, inputId1, middlePath, inputId2) {
            const input1 = document.getElementById(inputId1);
            const input2 = document.getElementById(inputId2);
            const value1 = input1.value.trim();
            const value2 = input2.value.trim();
            if (value1 && value2) {
                window.open(baseUrl + encodeURIComponent(value1) + middlePath + encodeURIComponent(value2), '_blank');
            } else {
                alert('Oopsie Woopsy Fucky Wucky... Enter a value in the text box.');
            }
        }
    </script>
    <style>
        .placeholder {
            color: #ef4444;
            font-weight: bold;
        }
    </style>
</head>
<body class=""bg-gray-100 min-h-screen p-8"">
    <div class=""container mx-auto"">
        <h1 class=""text-4xl font-bold text-center mb-2 text-gray-800"">XAU API Endpoints</h1>
        <p class=""text-center text-red-600 font-medium text-sm mb-6"">
            Warning: These endpoints are still in beta and have not been extensively tested. Use at your own risk!
        </p>
        <div class=""grid md:grid-cols-2 lg:grid-cols-3 gap-6"">
<!-- START ENDPOINTS -->
<!-- Profile Endpoints -->
            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/profile/me</h2>
                <p class=""text-gray-600 mb-4"">Get the current user's profile information</p>
                <a href=""/api/profile/me"" target=""_blank"" class=""btn bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"">Open Endpoint</a>
            </div>

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/profile/xuid/<span class=""placeholder"">{xuid}</span></h2>
                <p class=""text-gray-600 mb-4"">Get profile by XUID</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""xuidInput"" placeholder=""Enter XUID"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/profile/xuid/', 'xuidInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">Go</button>
                </div>
            </div>

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/profile/gt/<span class=""placeholder"">{gamertag}</span></h2>
                <p class=""text-gray-600 mb-4"">Get profile by Gamertag</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""gamertagInput"" placeholder=""Enter Gamertag"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/profile/gt/', 'gamertagInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">Go</button>
                </div>
            </div>

<!-- Games Endpoints -->
            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/games/me</h2>
                <p class=""text-gray-600 mb-4"">Get the current user's games list</p>
                <a href=""/api/games/me"" target=""_blank"" class=""btn bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"">Open Endpoint</a>
            </div>

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/games/xuid/<span class=""placeholder"">{xuid}</span></h2>
                <p class=""text-gray-600 mb-4"">Get games list by XUID</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""gamesXuidInput"" placeholder=""Enter XUID"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/games/xuid/', 'gamesXuidInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">Go</button>
                </div>
            </div>

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/games/gt/<span class=""placeholder"">{gamertag}</span></h2>
                <p class=""text-gray-600 mb-4"">Get games list by Gamertag</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""gamesGamertagInput"" placeholder=""Enter Gamertag"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/games/gt/', 'gamesGamertagInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">Go</button>
                </div>
            </div>

<!-- XAuth Endpoint -->
            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/xauth</h2>
                <p class=""text-gray-600 mb-4"">Get XAuth token</p>
                <a href=""/api/xauth"" target=""_blank"" class=""btn bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"">Plain Text Format</a>
                <a href=""/api/xauth?format=json"" target=""_blank"" class=""btn bg-yellow-500 text-white px-4 py-2 rounded hover:bg-yellow-600 ml-2"">JSON Format</a>
            </div>

<!-- Spoofing Endpoint -->

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/spoof/<span class=""placeholder"">{titleid}</span></h2>
                <p class=""text-gray-600 mb-4"">Spoofs the specified title for 5 minutes. Make an API request every 5 minutes to keep the spoofer working.</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""spoofIdInput"" placeholder=""Enter Title ID"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/spoof/', 'spoofIdInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">Go</button>
                </div>
            </div>

<!-- Achievements Endpoint -->
            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/achievements/<span class=""placeholder"">{titleid}</span></h2>
                <p class=""text-gray-600 mb-4"">Get achievements for a specific title</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""titleIdInput"" placeholder=""Enter Title ID"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/achievements/', 'titleIdInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">Go</button>
                </div>
            </div>


            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-green-500 text-white px-2 py-1 rounded text-sm"">POST</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/achievements/unlock</h2>
                <p class=""text-gray-600 mb-4"">Unlock achievements of title-based game by ID.</p>
                <div class=""flex space-x-4"">
                    <pre class=""bg-gray-800 text-white p-4 rounded-lg overflow-x-auto w-full whitespace-nowrap"">
                        <code class=""block"">{""titleId"": ""xxxxx"",""achievementId"": ""xxx""}</code>
                    </pre>
                </div>
            </div>

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-green-500 text-white px-2 py-1 rounded text-sm"">POST</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/achievements/unlockall</h2>
                 <p class=""text-gray-600 mb-4"">⚠️ BEWARE: Unlocks <u><strong>ALL</strong></u> achievements of a game ⚠️</p>
                <div class=""flex space-x-4"">
                    <pre class=""bg-gray-800 text-white p-4 rounded-lg overflow-x-auto w-full whitespace-nowrap"">
                        <code class=""block""> {""TitleId"": ""xxxxx""} </code>
                    </pre>
                </div>
            </div>
<!-- Game Search Endpoints -->

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/games/search/titleid/<span class=""placeholder"">{titleid}</span></h2>
                <p class=""text-gray-600 mb-4"">Retrieve game details for a specific Title ID.</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""searchTitleIdInput"" placeholder=""Enter Title ID"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/games/search/titleid/', 'searchTitleIdInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">GO</button>
                </div>
            </div>

            <div class=""bg-white rounded-xl shadow-lg p-6 relative"">
                <span class=""absolute top-4 right-4 bg-blue-500 text-white px-2 py-1 rounded text-sm"">GET</span>
                <h2 class=""text-xl font-semibold mb-4 text-gray-700"">/api/games/search/productid/<span class=""placeholder"">{productid}</span></h2>
                <p class=""text-gray-600 mb-4"">Retrieve game details for a specific product ID.</p>
                <div class=""flex items-center space-x-2"">
                    <input type=""text"" id=""searchProductIdInput"" placeholder=""Enter Product ID"" class=""flex-grow px-3 py-2 border rounded"">
                    <button onclick=""openEndpoint('/api/games/search/productid/', 'searchProductIdInput')"" class=""bg-gray-500 text-white px-4 py-2 rounded hover:bg-gray-600"">GO</button>
           </div>

<!-- END ENDPOINTS -->
        </div>
    </div>
</body>
</html>


";
        var buffer = Encoding.UTF8.GetBytes(htmlContent);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";

        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
}
