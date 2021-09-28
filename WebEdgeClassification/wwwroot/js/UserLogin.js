function prepareLists() {
    list('buckets', '/api/forge/oss/buckets');
}

var allBuckets = 'all';

function list(control, endpoint) {
    $('#' + control).find('option').remove().end();
    jQuery.ajax({
        url: endpoint,
        dataType: "json",
        multiple: false,
        data: function (node) {
            return {
                "id": node.id,
                "bucketName": allBuckets
            };
        },
        success: function (list) {
            if (list.length === 0)
                $('#' + control).append($('<option>', { disabled: true, text: 'Nothing found' }));
            else {
                list.forEach(function (item) {
                    $('#' + control).append($('<option>', { value: item.id, text: item.text }));
                })
            }
        }
    }).then(unblockElements);
}

function login() {
    $.post({
        url: 'api/forge/oauth/cred',
        contentType: 'application/json',
        data: JSON.stringify({
            ForgeClient: document.getElementById('forgeClientId').value,
            ForgeSecret: document.getElementById('forgeClientSecret').value
        }),
        success: function () {
            prepareLists();
        }
    });
}

function unblockElements() {
    var projSel = document.getElementById('projectSelector');
    projSel.style.display = 'block';
    var newBucket = document.getElementById('newBucket');
    newBucket.style.display = 'block';
}

var choosenBucket;

function selectProject() {
    choosenBucket = document.getElementById('buckets').value;
    var engSel = document.getElementById('projectSelector');
    engSel.style.display = 'none';
    var wndElem = document.getElementById('loginSetupWnd');
    wndElem.style.display = 'none';
    var newBucket = document.getElementById('newBucket');
    newBucket.style.display = 'none';
    openAndPrepareSecondWindow();
}

function createNewBucket() {
    var bucketKey = document.getElementById('forgeBucket').value;
    jQuery.post({
        url: '/api/forge/oss/buckets',
        contentType: 'application/json',
        data: JSON.stringify({
            'region': "US",
            'bucketKey': bucketKey
        }),
        success: function (res) {
            alert('New bucket has created');
            openAndPrepareSecondWindow();
        },
        error: function (err) {
            if (err.status == 409)
                alert('Bucket already exists - 409: Duplicated')
            console.log(err);
        }
    });
}

function openAndPrepareSecondWindow() {
    document.getElementById('loginPanel').style.display = 'none';
    document.getElementById('visualPanel').style.display = 'block';
    createAppBundleActivity();
    prepareAppBucketTree();
}
