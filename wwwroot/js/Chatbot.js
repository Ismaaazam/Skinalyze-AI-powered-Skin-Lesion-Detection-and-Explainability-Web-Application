/**
 * Chatbot.js  —  Skinalyze
 *
 * Call initChatbot("fullpage")  on Chatbot/Index
 * Call initChatbot("overlay")   on Home/Upload
 */

function initChatbot(mode) {
    const appWrapper = document.getElementById("appWrapper");
    const chatPanel = document.getElementById("chatPanel");
    const chatInput = document.getElementById("chatInput");
    const sendChat = document.getElementById("sendChat");
    const chatBody = document.getElementById("chatBody");

    /* ── helpers ─────────────────────────────────────── */
    function appendBubble(text, role) {
        const div = document.createElement("div");
        div.className = "bubble " + role;
        div.innerHTML = marked.parse(text);
        chatBody.appendChild(div);
        chatBody.scrollTop = chatBody.scrollHeight;
        return div;
    }

    function showTyping() {
        const dot = document.createElement("div");
        dot.className = "bubble typing";
        dot.id = "typingIndicator";
        dot.textContent = "Doctor AI is thinking...";
        chatBody.appendChild(dot);
        chatBody.scrollTop = chatBody.scrollHeight;
    }

    function removeTyping() {
        const dot = document.getElementById("typingIndicator");
        if (dot) dot.remove();
    }

    /* Real Flask API chatbot reply */
    async function botReply(userText) {

        showTyping();

        try {

            const API_URL =
                "https://web-production-55d23.up.railway.app/chat";

            const response =
                await fetch(
                    API_URL,
                    {
                        method: "POST",

                        headers: {
                            "Content-Type":
                                "application/json"
                        },

                        body: JSON.stringify({
                            message: userText
                        })
                    }
                );

            if (!response.ok) {

                throw new Error(
                    "API request failed"
                );

            }

            const data =
                await response.json();

            removeTyping();

            appendBubble(
                data.bot_response,
                "bot"
            );

        }
        catch (error) {

            console.error(error);

            removeTyping();

            appendBubble(
                "⚠️ Unable to reach chatbot server.",
                "bot"
            );

        }

    }

    /* ── Send message ─────────────────────────────────── */
    function handleSend() {
        const text = chatInput.value.trim();
        if (!text) return;
        appendBubble(text, "user");
        chatInput.value = "";
        botReply(text);
    }

    sendChat.addEventListener("click", handleSend);
    chatInput.addEventListener("keydown", function (e) {
        if (e.key === "Enter") {
            e.preventDefault();
            handleSend();
        }
    });

    /* ── Mode-specific setup ──────────────────────────── */
    if (mode === "overlay") {
        /* Overlay: triggered from Upload page's Suggestion button */
        const suggestBtn = document.getElementById("suggestBtn");
        const closeChat = document.getElementById("closeChat");

        if (suggestBtn) {
            suggestBtn.addEventListener("click", function () {
                appWrapper.classList.add("is-split");
                chatPanel.setAttribute("aria-hidden", "false");
                setTimeout(() => chatInput.focus(), 420);
            });
        }

        if (closeChat) {
            closeChat.addEventListener("click", function () {
                appWrapper.classList.remove("is-split");
                chatPanel.setAttribute("aria-hidden", "true");
            });
        }

    } else if (mode === "fullpage") {
        /* Full-page: panel is always visible, just focus input */
        chatPanel.setAttribute("aria-hidden", "false");
        chatInput.focus();
    }
}
