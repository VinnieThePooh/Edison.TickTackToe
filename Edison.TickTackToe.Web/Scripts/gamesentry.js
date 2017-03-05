$(function() {

    window.EnumMapper = function() {
        
        this.fromInt = {
            "0": "Idle",
            "1": "Playing",
            "2": "Offline"
        };

        this.enumMapper.fromString = {
            "Idle": 0,
            "Playing": 1,
            "Offline": 2
    };
    };

    console.log("was downloaded");
    var gamesHub = $.connection.gamesHub;

    // init
    defineClientCallbacks(gamesHub);



    $.connection.hub.start().done(function() {
        console.log("Hub: connection started");

        gamesHub.server.getOnlineUsers().done(function(users) {
                var table = $("#tableUsers");
                users.forEach(function(user) {
                    addUserToTable(table, user);
                });

            })
            .fail(function(error) {
                var par = $("#messageUsers");
                setTempMessage(par, "Could not load users list. Try to reload page later.");
                console.log(error.stack);
            });
    });
});



// difine own functions here
function addUserToTable(table, user) {
    var row = $("<tr>");
    // add icon too
    row.append($("<td>").text(user.Name));
    row.append($("<td>").text(user.Status));
    row.attr("data-email", user.Email);
    table.append(row);
}

function defineClientCallbacks(gamesHub) {
    var client = gamesHub.client;
    client.userJoinedSite = onUserJoinedSite;
    client.userLeftSite = onUserLeftSite;
    client.statusChanged = onStatusChanged;
}



function setTempMessage(paragraph, message, interval) {
    interval = interval || 3;
    paragraph.text(message);
    var counter = 0;

    var timer = setInterval(function () {
        if (counter++ == interval) {
            paragraph.text("");
            clearInterval(timer);
        };
    }, 1000);
}

function AcceptInvitation() {
   
}

function RejectInvitation() {
    
}



//
// callbacks
//
function onUserJoinedSite(data) {
    var email = data.Email;
    var name = data.Name;

    var tableUsers = $("#tableUsers");
    var targetRow = $("#tableUsers tr").filter(function () {
        return $(this).data("email") === email && $(this).find("td:eq(0)").text() === name;
    });

    if (!targetRow.length) {
        var row = $("<tr></tr>").attr("data-email", email);
        row.append($("<td></td>").text(name));
        row.append($("<td></td>").text(data.Status));
        tableUsers.append(row);
        setTempMessage($("#mu"), "User " + name + " joined the site");
    }
}


function onHandleInvitation(data) {
    
}


// not tested
function onStatusChanged(data) {
    if (data.Exception) {
        console.log(data.Exception);
        return;
    }

    var rows = $("#tableUsers").filter(function() {
        return $(this).data("email") === data.UserEmail;
    });

    if (rows) {
        var row = rows.first();
        row.find("td:eq(1)").text(window.EnumMapper.fromInt(data.StatusCode.toString()));

        var button = row.find("button");
        
        if (data.StatusCode === 1) {
            button.attr("disabled", true);
        } else {
            button.attr("disabled", false);
        }
    }
}


function onUserLeftSite(data) {
    var name = data.Name;
    var email = data.Email;

    var targetRow = $("#tableUsers tr").filter(function () {
        return $(this).data("email") === email && $(this).find("td:eq(0)").text() === name;
    });

    if (targetRow) {
        targetRow.remove();
        setTempMessage($("#mu"), "User " + name + " left the site");
    }
}