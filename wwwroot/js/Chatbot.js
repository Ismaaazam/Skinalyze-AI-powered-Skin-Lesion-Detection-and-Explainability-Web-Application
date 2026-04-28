/**
 * Chatbot.js  —  Skinalyze
 *
 * Call initChatbot("fullpage")  on Chatbot/Index
 * Call initChatbot("overlay")   on Home/Upload
 */

function initChatbot(mode) {
    const chatPanel = document.getElementById("chatPanel");
    const chatInput = document.getElementById("chatInput");
    const sendChat = document.getElementById("sendChat");
    const chatBody = document.getElementById("chatBody");

    /* ── helpers ─────────────────────────────────────── */
    function appendBubble(text, role) {
        const div = document.createElement("div");
        div.className = "bubble " + role;
        div.innerHTML = typeof marked !== "undefined" ? marked.parse(text) : text;
        chatBody.appendChild(div);
        chatBody.scrollTop = chatBody.scrollHeight;
        return div;
    }

    function showTyping() {
        const dot = document.createElement("div");
        dot.className = "bubble typing";
        dot.id = "typingIndicator";
        dot.textContent = "Doctor AI is thinking";
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
            const API_URL = "https://web-production-55d23.up.railway.app/chat";
            const response = await fetch(API_URL, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message: userText })
            });

            if (!response.ok) throw new Error("API request failed");

            const data = await response.json();
            removeTyping();
            appendBubble(data.bot_response, "bot");
        } catch (error) {
            console.error(error);
            removeTyping();
            appendBubble("⚠️ Unable to reach chatbot server. Please try again shortly.", "bot");
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

    if (sendChat) {
        sendChat.addEventListener("click", handleSend);
    }

    if (chatInput) {
        chatInput.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                handleSend();
            }
        });
    }

    /* ── Mode-specific setup ──────────────────────────── */
    if (mode === "overlay") {
        /* Floating FAB panel: toggled via the chat-fab button */
        const fab = document.getElementById("chatFab");
        const closeChat = document.getElementById("closeChat");
        const backdrop = document.getElementById("chatBackdrop");

        function openChat() {
            chatPanel.classList.add("chat-open");
            chatPanel.setAttribute("aria-hidden", "false");
            if (backdrop) backdrop.classList.add("active");
            setTimeout(() => chatInput && chatInput.focus(), 300);
        }

        function closePanel() {
            chatPanel.classList.remove("chat-open");
            chatPanel.setAttribute("aria-hidden", "true");
            if (backdrop) backdrop.classList.remove("active");
        }

        if (fab) fab.addEventListener("click", openChat);
        if (closeChat) closeChat.addEventListener("click", closePanel);
        if (backdrop) backdrop.addEventListener("click", closePanel);

        // Close on Escape
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && chatPanel.classList.contains("chat-open")) {
                closePanel();
            }
        });

    } else if (mode === "fullpage") {
        /* Full-page: panel is always visible */
        if (chatPanel) chatPanel.setAttribute("aria-hidden", "false");
        if (chatInput) chatInput.focus();
    }
}