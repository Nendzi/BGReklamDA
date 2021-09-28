
//function prepareLists() {
//    list('activity', '/api/forge/designautomation/activities');
//    list('engines', '/api/forge/designautomation/engines');
//    list('localBundles', '/api/appbundles');
//}

//function list(control, endpoint) {
//    $('#' + control).find('option').remove().end();
//    jQuery.ajax({
//        url: endpoint,
//        success: function (list) {
//            if (list.length === 0)
//                $('#' + control).append($('<option>', { disabled: true, text: 'Nothing found' }));
//            else
//                list.forEach(function (item) { $('#' + control).append($('<option>', { value: item, text: item })); })
//        }
//    });
//}

function clearAccount() {
    if (!confirm('Clear existing activities & appbundles before start. ' +
        'This is useful if you believe there are wrong settings on your account.' +
        '\n\nYou cannot undo this operation. Proceed?')) return;

    jQuery.ajax({
        url: 'api/forge/designautomation/account',
        method: 'DELETE',
        success: function () {
            prepareLists();
            writeLog('Account cleared, all appbundles & activities deleted');
        }
    });
}

function defineActivityModal() {
    $("#defineActivityModal").modal();
}

function createAppBundleActivity() {
    startConnection(function () {
        writeLog("Defining appbundle and activity for ");
        createAppBundle(function () {
            createActivity(function () {
                prepareLists();
            })
        });
    });
}

function createAppBundle(cb) {
    jQuery.ajax({
        url: 'api/forge/designautomation/appbundles',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            zipFileName: "EdgeClassificationConfig",
            engine: "InventorAutodesk.Inventor+2022"
        }),
        success: function (res) {
            writeLog('AppBundle: ' + res.appBundle + ', v' + res.version);
            if (cb) cb();
        }
    });
}

function createActivity(cb) {
    jQuery.ajax({
        url: 'api/forge/designautomation/activities',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            zipFileName: "EdgeClassificationConfig",
            engine: "Autodesk.Inventor+2022"
        }),
        success: function (res) {
            writeLog('Activity: ' + res.activity);
            if (cb) cb();
        }
    });
}

function startWorkItemForEdges() {
    startConnection(function () {
        var formData = new FormData();
        formData.append('forgeData', JSON.stringify({
            activityName: 'EdgeClassificationConfig',
            browerConnectionId: connectionId
        }));
        writeLog('Uploading input file to extract edges...');

        $.ajax({
            url: 'api/forge/designautomation/workitems/edges',
            data: formData,
            processData: false,
            contentType: false,
            type: 'POST',
            success: function (res) {
                writeLog('Workitem for edges started: ' + res.workItemId);
            }
        });
    });
}

function downloadEdges() {
    var bucketKey = 'EdgeClassificationConfig';
    var fileToDownload = 'edges.json';
    getForgeToken(function (access_token) {
        jQuery.get({
            url: 'https://developer.api.autodesk.com/oss/v2/buckets/2papxezihk45u80bhayrilhs7jqxwgr2-edgeclassificationconfig/objects/edges.json',
            headers: { 'Authorization': 'Bearer ' + access_token },
            contentType: 'application/json',
            dataType: 'json',
            success: function (res) { console.log(res); },
            error: function (err) { console.log(err); },
            complete: function (data) {
                allEdges = JSON.parse(data.responseText);
            }
        });
    });
}

function startWorkitem() {
    startConnection(function () {
        var formData = new FormData();
        var myJSONString = JSON.stringify(myShelf);
        formData.append('shelfData', myJSONString);
        formData.append('forgeData', JSON.stringify({
            activityName: 'EdgeClassificationConfig',
            browerConnectionId: connectionId
        }));
        writeLog('Uploading input file...');
        $.ajax({
            url: 'api/forge/designautomation/workitems',
            data: formData,
            processData: false,
            contentType: false,
            type: 'POST',
            success: function (res) {
                writeLog('Workitem started: ' + res.workItemId);
            }
        });
    });
}

function writeLog(text) {
    var elementName = "layerList";
    $('#' + elementName).append('<div style="border-top: 1px dashed #C0C0C0">' + text + '</div>');
    var elem = document.getElementById(elementName);
    elem.scrollTop = elem.scrollHeight;
}
function startConnection(onReady) {
    if (connection && connection.connectionState) { if (onReady) onReady(); return; }
    connection = new signalR.HubConnectionBuilder().withUrl("/api/signalr/designautomation").build();
    connection.start()
        .then(function () {
            connection.invoke('getConnectionId')
                .then(function (id) {
                    connectionId = id; // we'll need this...
                    if (onReady) onReady();
                });
        });

    connection.on("downloadResult", function (url) {
        writeLog('<a href="' + url + '">Download result file here</a>');
    });
    connection.on("onComplete", function (message) {
        writeLog(message);
        var bucketKey = 'EdgeClassificationConfig';
        var fileToDownload = 'edges.json';
        getForgeToken(function (access_token) {
            jQuery.get({
                url: 'https://developer.api.autodesk.com/oss/v2/buckets/2papxezihk45u80bhayrilhs7jqxwgr2-edgeclassificationconfig/objects/edges.json',
                headers: { 'Authorization': 'Bearer ' + access_token },
                contentType: 'application/json',
                dataType: 'json',
                success: function (res) { console.log(res); },
                error: function (err) { console.log(err); },
                complete: function (data) {
                    allEdges = JSON.parse(data.responseText);
                }
            });
        });
        $('#appBuckets').jstree(true).refresh();
    });

    connection.on("onProgress", function (message) {
        writeLog(message);
    });
}