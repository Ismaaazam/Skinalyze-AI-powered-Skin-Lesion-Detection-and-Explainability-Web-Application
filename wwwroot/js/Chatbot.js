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
        div.textContent = text;
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

    /* Simulated bot reply (replace with real API call later) */
    function botReply(userText) {
        showTyping();
        setTimeout(() => {
            removeTyping();
            const replies = [
                "Based on the description, I recommend consulting a dermatologist for a professional evaluation.",
                "Could you upload a clear image so I can provide better insights?",
                "Regarding the lesion: Based on the information provided, consistent monitoring is recommended.",
                "Please describe the lesion's color, size, and any changes you've noticed recently.",
                "I'm analyzing your query. For accurate results, please use the Upload feature to share an image."
            ];
            const reply = replies[Math.floor(Math.random() * replies.length)];
            appendBubble(reply, "bot");
        }, 1200);
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
