var viewer;
var edgeBuilder;

function launchViewer(urn) {
    var options = {
        env: 'AutodeskProduction',
        getAccessToken: getForgeToken
    };

    Autodesk.Viewing.Initializer(options, () => {
        viewer = new Autodesk.Viewing.GuiViewer3D(document.getElementById('forgeViewer'), { extensions: ['Autodesk.DocumentBrowser', 'Autodesk.Viewing.SceneBuilder'] });
        viewer.setTheme('light-theme');
        viewer.start();
        var documentId = 'urn:' + urn;
        Autodesk.Viewing.Document.load(documentId, onDocumentLoadSuccess, onDocumentLoadFailure);
    });
}

var compensation;

function onDocumentLoadSuccess(doc) {
    var geometryItems = doc.getRoot().search({ "role": "3d", "type": "geometry" });
    // Try 3D first
    if (geometryItems.length < 1) {
        geometryItems.push(doc.getRoot().getDefaultGeometry())
    }

    viewer.loadDocumentNode(doc, geometryItems[0]).then(i => {
        createModelBuilder().then(() => {
            compensation = NOP_VIEWER.model.getGlobalOffset();
            if (allEdges) {
                ShowAllEdges();
            }            
        });
    });
}

async function createModelBuilder() {
    await viewer.loadExtension('Autodesk.Viewing.SceneBuilder');
    var ext = viewer.getExtension('Autodesk.Viewing.SceneBuilder');
    edgeBuilder = await ext.addNewModel({
        conserveMemory: false,
        modelNameOverride: 'AllEdges'
    });
}

function onDocumentLoadFailure(viewerErrorCode, viewerErrorMsg) {
    console.error('onDocumentLoadFailure() - errorCode:' + viewerErrorCode + '\n- errorMessage:' + viewerErrorMsg);
}

function getForgeToken(callback) {
    fetch('/api/forge/oauth/token').then(res => {
        res.json().then(data => {
            callback(data.access_token, data.expires_in);
        });
    });
}