var gameManager = null;


$(function () {
    createUserFriendlyNotification();
    console.log("was downloaded");
    var gamesHub = $.connection.gamesHub;
    
    // init
    defineClientCallbacks(gamesHub);

    $.connection.hub.start().done(function() {
        console.log("Hub: connection started");

        gamesHub.server.getOnlineUsers().done(function(users) {
            var table = $("#tableUsers");
            var thead = $("<thead>");
            var tr = $("<tr>")
                    .append($("<th>").text(window.resources.usersTableHeadName))
                    .append($("<th>").text(window.resources.usersTableHeadStatus))
                    .append($("<th>"));
            thead.append(tr);
            table.append(thead);
            table.append($("<tbody>"));

            var notif = $("#userListNotif");
            notif.length && notif.remove();

                users.forEach(function(user) {
                    addUserToTable(table, user);
                });

            })
            .fail(function(error) {
                var par = $("#messageUsers");
                setTempMessage(par, window.resources.errorTimeoutUsersLoading);
                console.log(error.stack);
            });
    });

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


    function createUserFriendlyNotification() {
        var notif = $("<h5>").attr("id","userListNotif").addClass("text-info").addClass("text-center").text(window.resources.siteAreaNotifUsersLoading);
        $(".users").append(notif);
    }

    // difine own functions here
    function addUserToTable(table, user) {
        var row = $("<tr>");
        // add icon too
        row.append($("<td>").text(user.Name));
        row.append($("<td>").text(user.Status));


        // add button if current row doesnot match current user
        if ($("#tableUsers").data("name") !== user.Name) {
            var button = $("<button>").addClass("button button-default").height(30).text(window.resources.btnTextInviteUser);
            button.on("click", function () {
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
        client.playerRejectedToProceed = onPlayerRejectedToProceed;
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


    function findRowByUserName(name) {
        var rows = $("#tableUsers tr").filter(function () {
            return $(this).find("td:eq(0)").text() === name;
        });
        if (rows)
            return rows.first();
        return null;
    }


    function setDisableStateUsersTable(flag, exceptUserName) {

        if (flag === true)
            $("#tableUsers").find("button").each(function () { $(this).attr("disabled", flag); });
        else {

            var trs;
            if (exceptUserName)
                trs = $("#tableUsers tr").filter(function () {
                    return $(this).find("td:eq(1)").text() === EnumMapper.fromInt[0] &&
                           $(this).find("td:eq(0)").text() !== exceptUserName;
                });
            else trs = $("#tableUsers tr").filter(function () {
                return $(this).find("td:eq(1)").text() === EnumMapper.fromInt[0];
            });
            debugger;
            trs.find("button").attr("disabled", flag);
        }
    }


    function btnNoClickHandler(event) {

        var field = $("#field");
        field.length && field.remove();
        setDisableStateUsersTable(false, gameManager.getYourOnlineEnemyName());
        var gameId = gameManager.getGameId();

        // null - no value presents
        $.connection.gamesHub.server.beginNewGame(gameId, null).done(function () {
            console.log("decision to proceed sent");
        }).fail(function () {
            console.log("Failed while sending decision to proceed");
        });

        event.target.remove();
        var btnY = $("#btnYes");
        btnY.length && btnY.remove();
        $("#mg").text("");
        return true;
    }

    function createContinueButtons() {
        var btnY = $("<button>").attr("id", "btnYes").addClass("btn btn-default").text("Yes")
            .css("margin-right", "5px")
            .on("click", function (event) {

                var gameId = gameManager.getGameId();
                onPlayersStatusChanged({ StatusCode: 0, OpponentName: gameManager.getCurrentUserName(), InvitatorName: gameManager.getYourOnlineEnemyName() });

                event.target.remove();
                var btnN = $("#btnNo");
                btnN.length && btnN.remove();
                $("#mg").text("");

                // 1 - just flag that value is present, so  we wanna continue
                $.connection.gamesHub.server.beginNewGame(gameId, 1).done(function () {
                    console.log("new status was sent");
                }).fail(function () {
                    console.log("Failed while was sending status");
                });
            });

        var btnN = $("<button>").attr("id", "btnNo")
                    .addClass("btn btn-default")
                    .css("margin-right", "5px")
                    .text("No")
                    .on("click", btnNoClickHandler);

        $("#mg").append(btnN).append(btnY);

        var counter = 0;
        var timer = setInterval(function () {
            counter++;
            if (counter === 10) {

                var noButton = $("#btnNo");
                noButton.length && btnNoClickHandler() && noButton.remove();

                var yButton = $("#btnYes");
                yButton.length && yButton.remove();
                clearInterval(timer);
            }
        }, 1000);
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
        setTempMessage($("#mcontact"), window.resources.workflowUserRejectedInvitation.replace("#", data.UserName));
    }


    function onPlayerRejectedToProceed() {
        setTempMessage($("#mcontact"), window.resources.workflowUserRejectedToProceed.replace("#", gameManager.getYourOnlineEnemyName()), 3);
        var field = $("#field");
        field.length && field.remove();
        setDisableStateUsersTable(false);
    }

    function onUserAcceptedInvitation(data) {
        setTempMessage($("#mcontact"), window.resources.workflowUserAcceptedInvitation.replace("#", data.OpponentName));

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


    function onUserJoinedSite(data) {
        var email = data.Email;
        var name = data.Name;

        var tableUsers = $("#tableUsers");
        var targetRow = $("#tableUsers tr").filter(function () {
            return $(this).data("email") === email && $(this).find("td:eq(0)").text() === name;
        });

        if (!targetRow.length) {
            addUserToTable(tableUsers, data);
            setTempMessage($("#mu"), window.resources.siteAreaUserJoinedSite.replace("#", name));
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
        var oppName = data.OpponentName;
        var cuName = $("#tableUsers").data("name");
        // change status here
        // direct callback call to change status
        var par = { InvitatorName: invName, OpponentName: oppName, GameId: data.GameId, StatusCode: 1 };
        onPlayersStatusChanged(par);
        setDisableStateUsersTable(true);

        gameManager = new GameManager($.connection.gamesHub, invName, oppName, cuName, data.GameId, 3);
        gameManager.startGame();
    }


    // field size will be added
    function onHandleInvitation(data) {
        console.log("Got invitation from " + data.UserName);
        var mcontact = $("#mcontact");

        if ($("#btnAccept").length)
            return;

        mcontact.text(window.resources.workflowUserGotInvitation.replace("#", data.UserName)).attr("style", "font-size:12px;");
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

        var temp = $("<div>").attr("id", "temp").css("display", "inline-block");
        temp.append(btnReject).append(btnAccept);
        $(".contact").css("display", "block").append(temp);

        var counter = 0;
        window.timer = setInterval(function () {
            counter++;
            if (counter === 10) {
                var temp = $("#temp");
                if (temp) {
                    temp.remove();
                    $("#mcontact").text("");
                    btnReject.remove();
                    btnAccept.remove();
                    clearInterval(timer);
                }
            }
        }, 1000);
    }


    function onUserMadeStep(data) {
        var ci = data.ColumnIndex;
        var ri = data.RowIndex;
        var cell = $("#field").find(".cell[data-ri='" + ri + "'][data-ci='" + ci + "']");
        addFigureToCell(cell, gameManager.oppFigureName);

        gameManager.updateGameState(ri, ci, gameManager.oppFigureName);
        var oppName = gameManager.getYourOnlineEnemyName();
        var userWon = gameManager.resolveUser(oppName);
        if (userWon) {
            setTempMessage($("#mg"), window.resources.workflowUserLostTheGame, -1);
            createContinueButtons();
            return;
        }

        var cu = gameManager.getCurrentUserName();
        if (!gameManager.resolveUser(cu) && !gameManager.getUnsetItemsCount()) {
            $("#mg").text(window.resources.workflowDraw);
            createContinueButtons();
            return;
        }
        setTempMessage($("#mg"), window.resources.workflowYourTurn, -1);
        gameManager.inverseCanMakeStep();
    }


    function onHandleException(data) {
        console.log("Exception in " + data.MethodName + ":\n");
        console.log(data.Exception);
    }


    // not tested
    function onStatusChanged(data) {
        console.log("User[" + data.UserEmail + "] changed status to " + EnumMapper.fromInt[data.StatusCode]);
        var rows = $("#tableUsers tr").filter(function () {
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
            setTempMessage($("#mu"), window.resources.siteAreaUserLeftSite.replace("#", name));
        }
    }

    //
    // GameManager here
    //
    //todo: refactor
    function GameManager(hub, invName, oppName, curUserName, gid, fsize) {
        var instance = this;
        var gamesHub = hub;
        var invitatorName = invName;
        var currentUserName = curUserName;
        var gameId = gid;
        var fieldSize = fsize || 3;
        this.figureName = getFigureName(curUserName, oppName);
        this.oppFigureName = this.figureName === "nought" ? "cross" : "nought";
        var winnerUserName = null;
        var canMakeStep = curUserName === oppName;
        var map = createMatrix();

        this.lastMakeStepData = {};
        this.getUnsetItemsCount = function () {
            var counter = 0;
            for (var i = 0; i < fieldSize; i++)
                for (var j = 0; j < fieldSize; j++)
                    if (map[i][j] === -1)
                        counter++;
            return counter;
        };

        this.getGameId = function () {
            return gameId;
        };

        this.startGame = function () {
            createPlayingField();
            if (currentUserName === invitatorName)
                setTempMessage($("#mg"), window.resources.workflowOpponentFirst, -1);
            else
                setTempMessage($("#mg"), window.resources.workflowYouFirst, -1);

            var cells = $(".cell");
            cells.off();
            cells.mouseenter(function () {
                var th = $(this);
                console.log("was hovered");
                if (canMakeStep == false || th.hasClass(instance.figureName) || th.hasClass(instance.oppFigureName))
                    th.css("cursor", "not-allowed");
                else th.css("cursor", "default");
            });

            cells.on("click", function () {
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
                    setTempMessage($("#mg"), window.resources.workflowUserWonTheGame, -1);
                    createContinueButtons();
                    return;
                }

                if (!instance.getUnsetItemsCount()) {
                    $("#mg").text(window.resources.workflowDraw);
                    createContinueButtons();
                    return;
                }
                $("#mg").text(window.resources.workflowOpponentsTurn);
            });
        }

        this.getWinnerUserName = function () {
            return winnerUserName;
        }

        this.getOpponentName = function () {
            return oppName;
        };

        this.getCurrentUserName = function () {
            return currentUserName;
        }

        this.inverseCanMakeStep = function () {
            canMakeStep = !canMakeStep;
        };

        this.getImageUrl = function (figureName) {
            var base = "../Content/Images/";
            return figureName === "cross" ? base + "c2.png" : base + "n2.png";
        };

        this.getMap = function () {
            return map;
        }

        this.makeStep = function (i, j) {
            canMakeStep = !canMakeStep;
            // may be fail and done callbacks are redundant?
            gamesHub.server.makeStep(i, j, gameId).done(function () {
                console.log("figure sent");
            }).fail(function () {
                console.log("error while sending the figure");
            });

            var lastData = this.lastMakeStepData;
            lastData.i = i;
            lastData.j = j;
        };

        this.getYourOnlineEnemyName = function () {
            if (currentUserName === invitatorName)
                return oppName;
            return invitatorName;
        }


        this.updateGameState = function (i, j, figureName) {
            map[i][j] = figureName === "cross" ? 1 : 0;
            console.log("Cell[" + i + "," + j + "] was sent to: " + map[i][j]);
        };

        this.resolveUser = function (userName) {
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


            //todo:  remake with reusing
            var wrapper = $("#gameWrapper");
            wrapper.length && wrapper.remove();

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




});


