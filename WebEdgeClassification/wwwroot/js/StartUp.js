$(document).ready(function () {
    
    $('#clearAccount').click(clearAccount);
    $('#defineActivityShow').click(defineActivityModal);
    $('#createAppBundleActivity').click(createAppBundleActivity);
    $('#startWorkitemForEdges').click(startWorkItemForEdges);
    $('#downloadEdges').click(downloadEdges);
    $('#uploadEdges').click(uploadAllEdges);
    

    $('#login').click(login);
    $('#projectSelect').click(selectProject);
    $('#loginWithNewBucket').click(createNewBucket);

    $('#addEdgesToInside').click(putEdgesOnInside);
    $('#addEdgesToOutside').click(putEdgesOnOutside);
    $('#addEdgesToSweeping1').click(putEdgesOnSweeping1);
    $('#addEdgesToSweeping2').click(putEdgesOnSweeping2);
    $('#addEdgesToIgnore').click(putEdgesOnIgnore);

    $('#inputFile').change(function () {
        var node = $('#appBuckets').jstree(true).get_selected(true)[0];
        var _this = this;
        if (_this.files.length == 0) return;
        var file = _this.files[0];
        switch (node.type) {
            case 'bucket':
                var formData = new FormData();
                formData.append('fileToUpload', file);
                formData.append('bucketKey', node.id);

                $.ajax({
                    url: '/api/forge/oss/objects',
                    data: formData,
                    processData: false,
                    contentType: false,
                    type: 'POST',
                    success: function (data) {
                        $('#appBuckets').jstree(true).refresh_node(node);
                        _this.value = '';
                    }
                });
                break;
        }
    });

    startConnection();
});
