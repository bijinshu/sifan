function ajaxObject() {
    var xmlHttp;
    try {
        // Firefox, Opera 8.0+, Safari
        xmlHttp = new XMLHttpRequest();
    }
    catch (e) {
        // Internet Explorer
        try {
            xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (e) {
            try {
                xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (e) {
                alert("您的浏览器不支持AJAX！");
                return false;
            }
        }
    }
    return xmlHttp;
}
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
    },
    upload: function (url, files, onprogress, uploadComplete) {
        function inner(rsp) {
            uploadComplete(JSON.parse(rsp.target.response));
        }
        var xhr = ajaxObject();
        xhr.open("post", url, true); //post方式，url为服务器请求地址，true 该参数规定请求是否异步处理。
        xhr.onload = inner; //请求完成
        xhr.onerror = inner; //请求失败
        if (onprogress) {
            //【上传进度调用方法实现】
            xhr.upload.onprogress = function (evt) {
                if (evt.lengthComputable) {
                    onprogress(evt.loaded, evt.total);
                }
            };
            //上传开始执行方法
            xhr.upload.onloadstart = function () {
                onprogress(0, 100);
            };
        }
        var form = new FormData(); // FormData 对象
        form.append("files", files); // 文件对象
        xhr.send(form); //开始上传，发送form数据
    }
})