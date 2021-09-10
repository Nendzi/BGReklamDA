$(document).ready(function () {
    
    $('#clearAccount').click(clearAccount);
    $('#defineActivityShow').click(defineActivityModal);
    $('#createAppBundleActivity').click(createAppBundleActivity);
    $('#startWorkitem').click(startWorkitem);

    $('#login').click(login);
    $('#projectSelect').click(selectProject);
    $('#loginWithNewBucket').click(createNewBucket);

    startConnection();
});
