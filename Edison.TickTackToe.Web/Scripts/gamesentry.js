﻿var gameManager = null;
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
    client.userMadeStep = onUserMadeStep;
}



function setTempMessage(paragraph, message, interval, newMessage) {
    interval = interval || 3;
    paragraph.text(message);
    if (interval === -1)
        return;

    var counter = 0;

    var timer = setInterval(function () {
        if (counter++ === interval) {
            paragraph.text("");
            clearInterval(timer);
            paragraph.text(newMessage);
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
    gameManager = new GameManager($.connection.gamesHub, invName, oppName, invName, gameId, 3);
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


function addFigureToCell(cell, figureName) {
    cell.addClass(figureName);
    var container = $("<div>")
        .width(90)
        .height(90)
        .css("margin", "auto")
        .css("margin-top", "5px")
        .css("background-image", 'url(' + gameManager.getImageUrl(figureName) + ")")
        .css("background-repeat", "no-repeat");
    cell.append(container);
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

    gameManager = new GameManager($.connection.gamesHub, invName, oppName, oppName, data.GameId,3,true);
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


function onUserMadeStep(data) {
   
    // this one is added due to high intensive game consequences
    if (gameManager.getWinnerUserName())
        return;         

    var ci = data.ColumnIndex;
    var ri = data.RowIndex;

    var cell = $("#field").find(".cell[data-ri='" + ri + "'][data-ci='" + ci + "']");
    addFigureToCell(cell, gameManager.oppFigureName);

    gameManager.updateGameState(ci, ri, gameManager.oppFigureName);
    var oppName = gameManager.getYourOnlineEnemyName();
    if (gameManager.resolveUser(oppName)) {
        setTempMessage($("#mg"), "You lost. Wanna play once more?", -1);
        // u lost 
        // propose to play once more
        return;
    }
    setTempMessage($("#mg") , "Your turn to make a step",-1);
    gameManager.inverseCanMakeStep();
}


function onHandleException(data) {
    console.log("Exception in " + data.MethodName + ":\n");
    console.log(data.Exception);
}


// not tested
    function onStatusChanged(data) {

        var rows = $("#tableUsers").filter(function() {
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

        var targetRow = $("#tableUsers tr").filter(function() {
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
//todo: refactor
    function GameManager(hub, invName, oppName, curUserName, gid, fsize, cmStep) {
        var instance = this;
        var gamesHub = hub;
        var invitatorName = invName;
        var currentUserName = curUserName;
        var gameId = gid;
        var fieldSize = fsize || 3;
        this.figureName = getFigureName(curUserName, oppName);
        this.oppFigureName = this.figureName === "nought" ? "cross" : "nought";
        var winnerUserName = null;
        var canMakeStep = cmStep || false;
        var map = createMatrix();

        this.lastMakeStepData = {};

        this.getGameId = function() {
            return gameId;
        }

        this.startGame = function() {
            createPlayingField();
            if (currentUserName === invitatorName)
                setTempMessage($("#mg"), "Opponent makes a step first", -1);
            else
                setTempMessage($("#mg"), "You make first step", -1);

            var cells = $(".cell");
            cells.off();
            cells.mouseenter(function() {
                var th = $(this);
                console.log("was hovered");
                if (canMakeStep == false || th.hasClass(instance.figureName) || th.hasClass(instance.oppFigureName))
                    th.css("cursor", "not-allowed");
                else th.css("cursor", "default");
            });

            cells.on("click", function() {
                console.log("figure was clicked");
                var th = $(this);
                if (canMakeStep === false || th.hasClass(instance.figureName) || th.hasClass(instance.oppFigureName))
                    return;

                var i = th.data("ri");
                var j = th.data("ci");

                instance.updateGameState(i, j, instance.figureName);
                addFigureToCell(th, instance.figureName);
                var cUserName = instance.getCurrentUserName();
                var flag = instance.resolveUser(cUserName);
                instance.makeStep(i, j);
                if (flag) {
                    setTempMessage($("#mg"), "You won!!!. Wanna play once more?", -1);
                    // propose to play once more
                    return;
                }
                $("#mg").text("Opponent's turn to make step");
            });
        }

        this.getWinnerUserName = function() {
            return winnerUserName;
        }

        this.getOpponentName = function() {
            return oppName;
        };

        this.getCurrentUserName = function() {
            return currentUserName;
        }

        this.inverseCanMakeStep = function() {
            canMakeStep = !canMakeStep;
        };

        this.getImageUrl = function(figureName) {
            var base = "../Content/Images/";
            return figureName === "cross" ? base + "c2.png" : base + "n2.png";
        };

        this.makeStep = function(i, j) {
            canMakeStep = !canMakeStep;
            // may be fail and done callbacks are redundant?
            gamesHub.server.makeStep(i, j, gameId).done(function() {
                console.log("figure sent");
            }).fail(function() {
                console.log("error while sending the figure");
            });

            var lastData = this.lastMakeStepData;
            lastData.i = i;
            lastData.j = j;
        };

        this.getYourOnlineEnemyName = function() {
            if (currentUserName === invitatorName)
                return oppName;
            return invitatorName;
        }


        this.updateGameState = function(i, j, figureName) {
            map[i][j] = figureName === "cross" ? 1 : 0;
        };

        this.resolveUser = function(userName) {
            var fName = userName === currentUserName ? instance.figureName : instance.oppFigureName;
            var targetDigit = fName === "cross" ? 1 : 0;
            var won = checkHor(targetDigit) || checkVert(targetDigit) || checkDiag(targetDigit);
            if (won)
                winnerUserName = userName;
            return won;
        }

        // works only for 3 - size field yet
        function checkHor(targetDigit) {
            var flag;
            for (var i = 0; i < fieldSize; i++) {
                flag = true;
                for (var j = 0; j < fieldSize; j++)
                    if (map[i][j] !== targetDigit) {
                        flag = false;
                        break;
                    }
                if (flag === true)
                    return flag;
            }
            return flag;
        }

        function checkVert(targetDigit) {
            var flag;
            for (var i = 0; i < fieldSize; i++) {
                flag = true;
                for (var j = 0; j < fieldSize; j++)
                    if (map[j][i] !== targetDigit) {
                        flag = false;
                        break;
                    }
                if (flag === true)
                    return flag;
            }
            return flag;
        }

        function checkDiag(targetDigit) {
            return map[0][0] === targetDigit &&
                map[1][1] === targetDigit &&
                map[2][2] === targetDigit ||
                map[2][0] === targetDigit &&
                map[1][1] === targetDigit &&
                map[0][2] === targetDigit;
        }

        function createMatrix() {
            var newMatrix = [];
            for (var i = 0; i < fieldSize; i++) {
                newMatrix.push([]);
                for (var j = 0; j < fieldSize; j++) {
                    newMatrix[i].push(-1);
                }
            }
            return newMatrix;
        }

        // implementation details
        function getFigureName(cuName, oppName) {
            return cuName === oppName ? "cross" : "nought";
        }

        function createPlayingField(fieldSize) {
            fieldSize = fieldSize || 3;

            var gameWrapper = $("<div>")
                .attr("id", "gameWrapper")
                .css("width", "100%")
                .css("position", "relative");

            var table = $("<table>").attr("id", "field").addClass("table-bordered")
                .css("margin-top", "10px")
                .css("margin-bottom", "10px")
                .css("margin-left", "auto")
                .css("margin-right", "auto")
                .css("font-size", "16px")
                .addClass("text-info");


            var tbody = $("<tbody>");

            for (var i = 0; i < fieldSize; i++) {
                var row = $("<tr>");
                for (var j = 0; j < fieldSize; j++)
                    row.append($("<td>").addClass("cell").width(100).height(100)
                        .attr("data-ri", i)
                        .attr("data-ci", j));
                tbody.append(row);
            }

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



