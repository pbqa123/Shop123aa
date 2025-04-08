$(document).ready(function () {
    // Đóng chatbot
    $("#close-btn").click(function () {
        $("#chatBox").hide();
    });

    // Gửi tin nhắn khi nhấn nút "Gửi"
    $("#send-btn").click(function () {
        sendMessage();
    });

    // Gửi tin nhắn khi nhấn Enter
    $("#chatInput").keypress(function (e) {
        if (e.which == 13) {
            sendMessage();
        }
    });

    function sendMessage() {
        var message = $("#chatInput").val().trim();
        if (message === "") return;

        // Hiển thị tin nhắn người dùng
        var userMessage = `<div class="message message-sender">${message}</div>`;
        $("#messageContainer").append(userMessage);
        $("#chatInput").val(""); // Xóa input
        scrollToBottom();

        // Gửi yêu cầu đến server
        $.ajax({
            url: "/Chatbot/GetChatbotResponse",
            type: "POST",
            data: { message: message },
            success: function (response) {
                // Xử lý phản hồi từ chatbot
                var reply = response.reply;
                displayBotMessage(reply);
            },
            error: function () {
                displayBotMessage("Có lỗi xảy ra, vui lòng thử lại!");
            }
        });
    }

    function displayBotMessage(reply) {
        // Tách từng dòng trong phản hồi
        var lines = reply.split("\n");
        var messageHtml = '<div class="message message-receiver">';

        lines.forEach(function (line) {
            if (line.trim() === "") return; // Bỏ qua dòng trống

            // Kiểm tra nếu dòng chứa URL ảnh
            if (line.includes("Ảnh:") || line.includes("http") && (line.includes(".jpg") || line.includes(".png"))) {
                var imageUrl = line.split("Ảnh:")[1]?.trim() || line.trim();
                if (imageUrl) {
                    messageHtml += `<img src="${imageUrl}" class="thumbnail" alt="Product Image" />`;
                }
            } else if (line.includes("Xem chi tiết:")) {
                // Hiển thị link chi tiết
                var link = line.split("Xem chi tiết:")[1]?.trim();
                if (link) {
                    messageHtml += `<br><a href="${link}" target="_blank">Xem chi tiết sản phẩm</a>`;
                }
            } else {
                // Hiển thị văn bản thông thường
                messageHtml += `<p>${line}</p>`;
            }
        });

        messageHtml += "</div>";
        $("#messageContainer").append(messageHtml);
        scrollToBottom();
    }

    function scrollToBottom() {
        $("#chatBody").scrollTop($("#chatBody")[0].scrollHeight);
    }
});