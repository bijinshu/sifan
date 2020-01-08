$.extend({
    json: function (url, data, always) {
        data = data || {}
        $.ajax({
            type: "POST",
            url: url,
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(data),
            success: function (resp) {
                if (always) {
                    always(resp);
                }
            },
            error: function (err) {
                if (always) {
                    always(err);
                }
            }
        })
    }
})