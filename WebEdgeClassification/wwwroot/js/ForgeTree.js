function prepareAppBucketTree() {
    $('#appBuckets').jstree({
        'core': {
            'themes': { "icons": true },
            'data': {
                "url": '/api/forge/oss/buckets',
                "dataType": "json",
                'multiple': false,
                "data": function (node) {
                    return {
                        "id": node.id,
                        "bucketName": choosenBucket
                    };
                }
            }
        },
        'types': {
            'default': {
                'icon': 'glyphicon glyphicon-question-sign'
            },
            '#': {
                'icon': 'glyphicon glyphicon-cloud'
            },
            'bucket': {
                'icon': 'glyphicon glyphicon-gift',
                'valid_children': ['object']
            },
            'object': {
                'icon': 'glyphicon glyphicon-file'
            }
        },
        'plugins': ["types", "state", "sort", "contextmenu"],
        contextmenu: { items: autodeskCustomMenu }
    }).on('loaded.jstree', function () {
        $('#appBuckets').jstree('open_all');
    }).bind("activate_node.jstree", function (evt, data) {
        if (data != null && data.node != null && data.node.type == 'object') {
            selectedPart = data.node.original.text;
            $("#forgeViewer").empty();
            var urn = data.node.id;
            getForgeToken(function (access_token) {
                jQuery.ajax({
                    url: 'https://developer.api.autodesk.com/modelderivative/v2/designdata/' + urn + '/manifest',
                    headers: { 'Authorization': 'Bearer ' + access_token },
                    success: function (res) {
                        if (res.status === 'success') launchViewer(urn);
                        else $("#forgeViewer").html('The translation job still running: ' + res.progress + '. Please try again in a moment.');
                    },
                    error: function (err) {
                        var msgButton = 'This file is not translated yet! ' +
                            '<button class="btn btn-xs btn-info" onclick="translateObject()"><span class="glyphicon glyphicon-eye-open"></span> ' +
                            'Start translation</button>'
                        $("#forgeViewer").html(msgButton);
                    }
                });
            })
        }
    });
}
var selectedPart;

function autodeskCustomMenu(autodeskNode) {
    var items;

    switch (autodeskNode.type) {
        case "bucket":
            items = {
                uploadFile: {
                    label: "Upload file",
                    action: function () {
                        uploadFile();
                    },
                    icon: 'glyphicon glyphicon-cloud-upload'
                },
                deleteBucket: {
                    label: "Delete bucket",
                    action: function () {
                        deleteBucket();
                    },
                    icon: 'glyphicon glyphicon-trash'
                }
            };
            break;
        case "object":
            items = {
                translateFile: {
                    label: "Translate",
                    action: function () {
                        var treeNode = $('#appBuckets').jstree(true).get_selected(true)[0];
                        translateObject(treeNode);
                    },
                    icon: 'glyphicon glyphicon-eye-open'
                },
                deleteFile: {
                    label: "Delete",
                    action: function () {
                        var treeNode = $('#appBuckets').jstree(true).get_selected(true)[0];
                        deleteFile(treeNode);
                    },
                    icon: 'glyphicon glyphicon-remove'
                }
            };
            break;
    }
    return items;
}

function uploadFile() {
    $('#inputFile').click();
}

function downloadObject(node) {
    var bucketKey = node.parent;
    var objectName = node.text;
    jQuery.ajax({
        url: '/api/forge/objects/signed',
        contentType: 'application/json',
        data: JSON.stringify({ 'bucketKey': bucketKey, 'fileToDownload': objectName }),
        type: 'POST',
        success: function (res) {
            document.getElementById('responseMessage').innerHTML = '<a href="' + res.signedUrl + '">Download result file here</a>';
        },
        error: function (err) { console.log(err); }
    });
}

function deleteBucket() {
    var node = $('#appBuckets').jstree(true).get_selected(true)[0];
    jQuery.ajax({
        url: '/api/forge/oss/buckets',
        contentType: 'application/json',
        data: JSON.stringify({ 'bucketKey': node.id }),
        type: 'DELETE',
        success: function (res) {
            $('#appBuckets').jstree(true).refresh();
        },
        error: function (err) { console.log(err); }
    });
}

function translateObject(node) {
    $("#forgeViewer").empty();
    if (node == null) node = $('#appBuckets').jstree(true).get_selected(true)[0];
    var bucketKey = node.parents[0];
    var objectKey = node.id;
    jQuery.post({
        url: '/api/forge/modelderivative/jobs',
        contentType: 'application/json',
        data: JSON.stringify({ 'bucketKey': bucketKey, 'objectName': objectKey }),
        success: function (res) {
            $("#forgeViewer").html('Translation started! Please try again in a moment.');
        },
    });
}

function deleteFile(node) {
    $("#forgeViewer").empty();
    var node = $('#appBuckets').jstree(true).get_selected(true)[0];
    var bucketKey = node.parent;
    var objectKey = node.text;

    jQuery.ajax({
        url: '/api/forge/oss/objects/delete',
        contentType: 'application/json',
        data: JSON.stringify({ 'bucketKey': bucketKey, 'objectKey': objectKey }),
        type: 'DELETE',
        success: function (res) {
            $('#appBuckets').jstree(true).refresh();
        },
        error: function (err) { console.log(err); }
    });
}