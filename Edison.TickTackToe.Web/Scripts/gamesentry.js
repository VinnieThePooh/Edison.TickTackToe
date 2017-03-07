var gameManager = null;
var EnumMapper = {

    fromInt: {
        "0": "Idle",
        "1": "Playing",
        "2": "Offline"
    },

    fromString: {
        "Idle": 0,
        "Playing": 1,
        "Offline": 2
    }
};


$(function () {
    
    console.log("was downloaded");
    var gamesHub = $.connection.gamesHub;

    // init
    defineClientCallbacks(gamesHub);
    initHandlers();

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

function  initHandlers()
{
    
}



// difine own functions here
function addUserToTable(table, user) {
    var row = $("<tr>");
    // add icon too
    row.append($("<td>").text(user.Name));
    row.append($("<td>").text(user.Status));


    // add button if current row doesnot match current user
    if ($("#tableUsers").data("name") !== user.Name) {

        var button = $("<button>").addClass("button button-default").height(30).text("Invite");
        button.on("click", function() {
            var row = $(this).closest("tr");
            var email = row.data("email");
            console.log("User with email: " + email + " clicked");
            $.connection.gamesHub.server.inviteUser(row.find("td:eq(0)").text());
        });

        row.append($("<td>").append(button));
        if (user.Status !== "Idle" || gameManager)
            button.attr("disabled", true);
        else button.attr("disabled", false);
    } else {
        row.append("<td>");
    }

    row.attr("data-email", user.Email);
    table.append(row);
}

function defineClientCallbacks(gamesHub) {
    var client = gamesHub.client;
    
    client.userJoinedSite = onUserJoinedSite;
    client.userLeftSite = onUserLeftSite;
    client.statusChanged = onStatusChanged;
    client.invitationArrived = onHandleInvitation;
    client.handleException = onHandleException;
    client.userAcceptedInvitation = onUserAcceptedInvitation;
    client.userRejectedInvitation = onUserRejectedInvitation;
    client.beginNewGame = onBeginNewGame;
    client.playersStatusChanged = onPlayersStatusChanged;
}



function setTempMessage(paragraph, message, interval) {
    interval = interval || 3;
    paragraph.text(message);
    if (interval === -1)
        return;

    var counter = 0;

    var timer = setInterval(function () {
        if (counter++ == interval) {
            paragraph.text("");
            clearInterval(timer);
        };
    }, 1000);
}


function onUserAcceptedInvitation(data) {
    setTempMessage($("#mcontact"), data.OpponentName + " accepted your invitation");
    
    // changes status here
    var invName = $("#tableUsers").data("name");
    var oppName = data.OpponentName;

    // direct callback call to change status
    var par = { InvitatorName: invName, OpponentName: oppName, GameId: data.GameId, StatusCode: 1 };
    onPlayersStatusChanged(par);
    setDisableStateUsersTable(true);

    // create manager here to track new game
    var gameId = data.GameId;
    gameManager = new GameManager($.connection.gamesHub, invName, oppName, invName, gameId);
    gameManager.startGame();
}


function findRowByUserName(name) {
    var rows = $("#tableUsers tr").filter(function () {
        return $(this).find("td:eq(0)").text() === name;
    });
    if (rows)
        return rows.first();
    return null;
}


function setDisableStateUsersTable(flag) {
    $("#tableUsers").find("button").each(function() {
        $(this).attr("disabled",flag);
    });
}



//
// callbacks
//
function onUserRejectedInvitation(data) {
    setTempMessage($("#mcontact"), data.UserName + " rejected your invitation");
}

function onUserJoinedSite(data) {
    var email = data.Email;
    var name = data.Name;

    var tableUsers = $("#tableUsers");
    var targetRow = $("#tableUsers tr").filter(function () {
        return $(this).data("email") === email && $(this).find("td:eq(0)").text() === name;
    });

    if (!targetRow.length) {
        addUserToTable(tableUsers, data);
        setTempMessage($("#mu"), "User " + name + " joined the site");
    }
}


function onPlayersStatusChanged(data) {
    console.log("onPlayersStatusChanged was called");

    var invName = data.InvitatorName;
    var oppName = data.OpponentName;
    var statusCode = data.StatusCode;

    var status = EnumMapper.fromInt[statusCode];

    var invRow = findRowByUserName(invName);
    if (invRow.length) {
        invRow.find("td:eq(1)").text(status);
        var button = invRow.find("button");
        button.length && button.attr("disabled", true);
    }

    var oppRow = findRowByUserName(oppName);
    if (oppRow.length) {
        oppRow.find("td:eq(1)").text(status);
        var oppButton = oppRow.find("button");
        oppButton.length && oppButton.attr("disabled", true);
    }
}



function onBeginNewGame(data) {
    var invName = data.InvitatorName;
    // currentUser
    var oppName = $("#tableUsers").data("name");

    // change status here
    // direct callback call to change status
    var par = { InvitatorName: invName, OpponentName: oppName, GameId: data.GameId, StatusCode: 1 };
    onPlayersStatusChanged(par);
    setDisableStateUsersTable(true);

    gameManager = new GameManager($.connection.gamesHub, invName, oppName, oppName, data.GameId);
    gameManager.startGame();
}


// field size will be added
function onHandleInvitation(data) {
    console.log("Got invitation from " + data.UserName);
    var mcontact = $("#mcontact");

    if ($("#btnAccept").length)
        return;

    mcontact.text(data.UserName + " has invited u in game. Wanna accept?").attr("style", "font-size:12px;");
    
    var btnAccept = $("<button>").addClass("btn btn-default").attr("id", "btnAccept").on("click", function () {
        // send to server
        $.connection.gamesHub.server.acceptInvitation(data.UserName);

        // clear markup
        var temp = $(".contact").find("#temp");
        if (temp)
            temp.remove();

        if (window.timer) {
            clearInterval(window.timer);
            window.timer = null;
        }
        $("#mcontact").text("");
        console.log("accept clicked");
    });
    btnAccept.append($("<span>").text("Accept").attr("style", "font-size:12px;"));



    var btnReject = $("<button>").addClass("btn btn-default").attr("id", "btnReject").on("click", function () {

        // send to server
        $.connection.gamesHub.server.rejectInvitation(data.UserName);

        // clear markup
        var temp = $(".contact").find("#temp");
        if (temp)
            temp.remove();

        if (window.timer) {
            clearInterval(window.timer);
            window.timer = null;
        }

        $("#mcontact").text("");
        console.log("reject clicked");
    });
//    btnReject.height(30);
    btnReject.append($("<span>").text("Reject").attr("style", "font-size:12px;"));
    btnReject.css("margin-right", "3px");

    var temp = $("<div>").attr("id", "temp").css("display","inline-block");
    temp.append(btnReject).append(btnAccept);
    $(".contact").css("display", "block").append(temp);

    var counter = 0;
    window.timer = setInterval(function() {
        counter++;
        if (counter === 10) {
            var temp = $("#temp");
            if (temp) {
                temp.remove();
                $("#mcontact").text("");
                clearInterval(timer);
            }
        }
    }, 1000);

}


function onHandleException(data) {
    console.log("Exception in " + data.MethodName + ":\n");
    console.log(data.Exception);
}


// not tested
function onStatusChanged(data) {

    var rows = $("#tableUsers").filter(function () {
        return $(this).data("email") === data.UserEmail;
    });

    if (rows) {
        var row = rows.first();
        row.find("td:eq(1)").text(EnumMapper.fromInt[data.StatusCode]);

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


//
// GameManager here
//
function GameManager(hub, invName, oppName, curUserName, gid, fsize) {
    var gamesHub = hub;
    var invitatorName = invName;
    var currentUserName = curUserName;
    var gameId = gid;
    var fieldSize = fsize || 3;
    var figureName = getFigureName(curUserName, oppName);
    var oppFigureName = figureName === "nought" ? "cross" : "nought";

    this.startGame = function() {
        createPlayingField();
        if (currentUserName === invitatorName)
            setTempMessage($("#mg"), "Opponent makes a step first", -1);
        else 
            setTempMessage($("#mg"), "Your make first step", -1);

        var cells = $(".cell");
        cells.off();
        cells.mouseenter(function() {
            var th = $(this);
            console.log("was hovered");
            if (th.hasClass(figureName) || th.hasClass(oppFigureName))
                th.css("cursor", "not-allowed");
            else th.css("cursor", "default");
        });
        cells.on("click", function () {
            console.log("figure was clicked");
            var th = $(this); 
            if (th.hasClass(figureName) || th.hasClass(oppFigureName))
                return;

            th.addClass(figureName);

            var container = $("<div>")
                .width(90)
                .height(90)
                .css("margin", "auto")
                .css("margin-top", "4px")
                .css("background-image", 'url(' + getImageUrl() + ")")
                .css("background-repeat", "no-repeat");
            th.append(container);
        });

        
    }

    function getImageUrl() {
        var base = "../Content/Images/";
        return figureName === "cross" ? base + "c2.png" : base + "n2.png";
    }
    
    this.makeStep = function (figureId) {
        // hub does something +
        // + gui
    }

    // implementation details
    function getFigureName(cuName, oppName) {
        return cuName === oppName ? "nought" : "cross";
    }
    
    function createPlayingField(fieldSize) {
        fieldSize = fieldSize || 3;

        var gameWrapper = $("<div>")
            .attr("id", "gameWrapper")
            .css("width", "100%")
            .css("position","relative");

        var table = $("<table>").attr("id", "field").addClass("table-bordered")
            .css("margin-top", "10px")
            .css("margin-bottom", "10px")
            .css("margin-left", "auto")
            .css("margin-right", "auto")
            .css("font-size", "16px")
            .addClass("text-info");

        
        var tbody = $("<tbody>");

        for (var index = 0; index < fieldSize; index++) {
            var row = $("<tr>");
            for (var j = 0; j < fieldSize; j++)
                row.append($("<td>").addClass("cell").width(100).height(100));
            tbody.append(row);
        }

        tbody.find("td").last().addClass("nought");

        table.append(tbody);

        var existed = $(".game").find("table#field");

        // todo: remake
        // or just reuse
        if (existed)
            existed.remove();

        var span = $("<span>").attr("id", "mg")
            .css("float", "left")
            .css("font-size", "16px")
            .css("max-width", "300px")
            .css("position", "absolute")
            .css("left", "5px")
            .addClass("text-info");

        gameWrapper.append(span);
        gameWrapper.append(table);

        return $(".game").append(gameWrapper);
    }
}
