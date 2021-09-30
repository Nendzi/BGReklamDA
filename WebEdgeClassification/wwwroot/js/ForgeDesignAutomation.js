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
        //writeLog("Defining appbundle and activity for ");
        createAppBundle(function () {
            createActivity("alfa", function() {
                createActivity("bravo", function () {
                    prepareLists();
                })
            });
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
            //writeLog('AppBundle: ' + res.appBundle + ', v' + res.version);
            if (cb) cb();
        }
    });
}

function createActivity(version, cb) {
    jQuery.ajax({
        url: 'api/forge/designautomation/activities',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            version: version,
            engine: "Autodesk.Inventor+2022"
        }),
        success: function (res) {
            //writeLog('Activity: ' + res.activity);
            if (cb) cb();
        }
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

function uploadEdgesToBucket() {
    var edgesForBucket = JSON.stringify(separatedEdges);
    getForgeToken(function (access_token) {
        $.ajax({
            url: 'https://developer.api.autodesk.com/oss/v2/buckets/2papxezihk45u80bhayrilhs7jqxwgr2-edgeclassificationconfig/objects/layers.json',
            type: 'PUT',
            headers: { 'Authorization': 'Bearer ' + access_token },
            contentType: 'application/json',
            dataType: 'json',
            data: edgesForBucket,
            success: function (res) { console.log(res); },
            error: function (err) { console.log(err); },
            complete: function (data) {
                console.log('It is finished');
            }
        });
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
                //writeLog('Workitem for edges started: ' + res.workItemId);
            }
        });
    });
}

function startWorkitem() {
    startConnection(function () {
        var formData = new FormData();
        var myJSONString = JSON.stringify(separatedEdges);
        formData.append('edgeData', myJSONString);
        formData.append('forgeData', JSON.stringify({
            fileName: selectedPart,
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
                //writeLog('Workitem started: ' + res.workItemId);
            }
        });
    });
}

function writeLog(text) {
    var elementName = "infoPanel";
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
        var dwnlbtn = document.getElementById('downLoadResult');
        dwnlbtn.innerHTML='<a href="' + url + '">Download result file here</a>';
    });
    connection.on("onComplete", function (message) {
        //writeLog(message);
        console.log(message);
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
                    //allEdges = JSON.parse(data.responseText);
                }
            });
        });
        $('#appBuckets').jstree(true).refresh();
    });

    connection.on("onProgress", function (message) {
        //writeLog(message);
    });
}