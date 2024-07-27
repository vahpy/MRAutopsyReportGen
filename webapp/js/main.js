/* Important notes:
* 1. PNG used for CT images, and JPEG for regular pictures
*/

//global variables
var ws1, ws2;
const canvas = document.getElementById('myCanvas');
const ctx = canvas.getContext('2d');
canvas.width = window.innerWidth;
canvas.height = window.innerHeight;
var prev_cursor_x = NaN;
var prev_cursor_y = NaN;
var cursor_x = 100; // Initial x position of the cursor
var cursor_y = 100; // Initial y position of the cursor
var isDrawingLine = false;
var draggedElement = null;
var lastDraggedElement = null;
// Set the target width and height for the animation
var max_width = 1280;

var originalWidth = 1280;
var originalHeight = 720;
var targetWidth = 320;
var targetHeight = 180;

// Set the duration of the animation in milliseconds
var duration = 1000;
var qrcodeTimeout = 3000;
var connectionWait = 5000;

// effects
var prev_hover_element = null;



// MultiUser Interaction
var last_user = "";
var currentSectionForUser = {};
var users = [];
//new inputs
var transform = "";
var frameNumber = 0;
var filePath = "";
var isEditing = false;

// Func
function editReport() {
  if (isEditing) {
    isEditing = false;
    $(".subContainer").addClass("ui-sortable");
    $(".voice_transcript").addClass("ui-sortable-handle");
    $(".voice_transcript").attr("contenteditable", "false");
    $(".voice_transcript").attr("draggable", "true");
  } else {
    isEditing = true;
    $(".subContainer").removeClass("ui-sortable");
    $(".voice_transcript").removeClass("ui-sortable-handle");
    $(".voice_transcript").attr("contenteditable", "true");
    $(".voice_transcript").attr("draggable", "false");
  }
}


// Drag from browser to MR
var lastDraggedElementToMR = null;

function getCurrentSection(user) {
  let section = currentSectionForUser[user];
  if (!section) {
    section = document.getElementById("lastCreatedSection");
    if (!section) {
      addNewSection();
      section = document.getElementById("lastCreatedSection");
    }
    currentSectionForUser[user] = section;
  }
  return section;
}

function setCurrentSection(section) {
  currentSectionForUser[last_user] = section;
}

// Dynamic web page elements

function addImage(user, imageData, source, imageType = 'image/jpeg') {
  if (imageType === "base64") {
    addImageByBase64(user, imageData, source, transform);
    return;
  }
  const blob = new Blob([imageData], { type: imageType });
  blobToBase64(blob).then(base64 => addImageByBase64(user, base64, source, transform));
}

function addImageByBase64(user, base64, source, transformPar = [null]) {
  // create a div
  let section = getCurrentSection(user);
  let subContainer = section.getElementsByClassName("subContainer")[0];

  let imgElement = document.createElement('img');
  imgElement.draggable = true;
  subContainer.appendChild(imgElement);
  imgElement.src = base64;
  //console.log("data-imgtaker: " + user);
  // fetch the index of the user in the users list
  imgElement.classList.add("user" + users.indexOf(user));
  imgElement.setAttribute("data-imgtaker", user);
  imgElement.setAttribute("data-source", source);
  imgElement.setAttribute("data-framenumber", frameNumber);
  imgElement.setAttribute("data-filepath", filePath);
  if (transformPar != null && transformPar.length > 0) imgElement.setAttribute("data-transform", transformPar);

  originalWidth = imgElement.width;
  originalHeight = imgElement.height;
  console.log("originalWidth: " + originalWidth + " originalHeight: " + originalHeight)
  imgElement.width = Math.min(originalWidth, max_width);
  imgElement.height = Math.min(originalHeight, max_width / (originalWidth / originalHeight));
  console.log("imgElement.width: " + imgElement.width + " imgElement.height: " + imgElement.height)

  makeAllImagesRightSize();
}

function makeAllImagesRightSize() {
  let images = document.querySelectorAll(".subContainer img");
  for (let i = 0; i < images.length; i++) {
    let img = images[i];
    originalWidth = img.width;
    originalHeight = img.height;
    if (originalWidth != 0 && originalHeight != 0) targetHeight = targetWidth / (originalWidth / originalHeight);
    else {
      targetWidth = 320;
      targetHeight = 180;
      originalWidth = 1280;
      originalHeight = 720;
    }
    img.width = targetWidth;
    img.height = targetHeight;
  }
}

function addWordsToCurrentSection(user, words) {
  let section = getCurrentSection(user);

  let subContainer = section.getElementsByClassName("subContainer")[0];
  if (!subContainer) return;
  let textFields = subContainer.querySelectorAll("[owner=sp-" + user + "]");
  if (!textFields) {
    let span = document.createElement("span");
    span.className = "voice_transcript";
    span.classList.add("user" + users.indexOf(user));
    span.setAttribute("owner", "sp-" + user);
    span.draggable = true;
    span.innerHTML = "<b>" + user + ":</b> " + words;
    subContainer.appendChild(span);
    return;
  }
  let textField = textFields[textFields.length - 1];
  if (textField) {
    let sibling = textField.nextElementSibling;
    if (sibling) console.log(sibling.tagName);
    if (sibling && (sibling.tagName == "SPAN" || sibling.tagName == "IMG")) {
      let span = document.createElement("span");
      span.className = "voice_transcript";
      span.classList.add("user" + users.indexOf(user));
      span.setAttribute("owner", "sp-" + user);
      span.draggable = true;
      span.innerHTML = "<b>" + user + ":</b> ";
      subContainer.appendChild(span);
    }
    textField.innerHTML = textField.innerHTML + words;
  }
  else {
    let span = document.createElement("span");
    span.className = "voice_transcript";
    span.setAttribute("owner", "sp-" + user);
    span.draggable = true;
    span.innerHTML = "<b>" + user + ":</b> " + words;
    section.getElementsByClassName("subContainer")[0].appendChild(span);
  }
}

function addNewSection(user) {
  while (true) {
    let prev_sec = document.getElementById("lastCreatedSection");
    if (prev_sec) {
      prev_sec.id = "";
    }
    else {
      break;
    }
  }
  let section = document.createElement("div");
  section.id = "lastCreatedSection";
  section.className = "dataContainer";
  document.getElementById("content_body").appendChild(section);
  //<span class="voice_transcript" draggable></span>
  section.innerHTML = '<div class="subContainer"></div><div><button class="selectbutton">Select</button></div></div>';
  makeAllContainersSortable();
  // Add the event listener for the add button
  section.getElementsByClassName("selectbutton")[0].addEventListener("mousedown", function () {
    console.log("select button clicked");
    setCurrentSection(section);
  });
  //
  if (user) {
    currentSectionForUser[user] = section;
  }
}

function makeAllContainersSortable() {
  $(".subContainer").sortable({
    connectWith: ".subContainer img, .subContainer span, .subContainer, .ctviewerImg img, .ctviewerImg",
    start: function (e, ui) {
      ui.placeholder.height(ui.item.height());
      ui.placeholder.width(ui.item.width());

      //show the delete icon
      $('#recyclebinarea').show();
      $('#recyclebinarea').hover(function () {
        $('#recyclebinarea>img').css('width', '150px');
        $('#recyclebinarea>img').css('height', '150px');
      }, function () {
        $('#recyclebinarea>img').css('width', '50px');
        $('#recyclebinarea>img').css('height', '50px');
      }
      );
    },
    beforeStop: function (e, ui) {
      let recyclebinarea = document.getElementById("recyclebinarea");
      if ($('#recyclebinarea').is(':hover') || document.elementsFromPoint(cursor_x, cursor_y).includes(recyclebinarea)) {
        console.log("Dragged object deleted");
        ui.item.remove();
      }
      //hide the delete icon
      $('#recyclebinarea').hide();
    }
  });
  //makeCTViewDraggable();
  $(".ctviewerImg").sortable({
    connectWith: ".subContainer img, .subContainer span, .subContainer, .ctviewerImg img, .ctviewerImg",
    start: function (e, ui) {
      //
      originalWidth = ui.item.width();
      originalHeight = ui.item.height();
      if (originalWidth != 0 && originalHeight != 0) targetHeight = targetWidth / (originalWidth / originalHeight);
      else {
        targetWidth = 320;
        targetHeight = 180;
        originalWidth = 1280;
        originalHeight = 720;
      }
      // img.width = targetWidth;
      // img.height = targetHeight;
      ui.item.height(targetHeight);
      ui.item.width(targetWidth);
    }
    ,
    helper: function (event, ui) {
      // create a copy of the dragged item and add a placeholder class
      clone = $(ui).clone();
      ui.after(clone);
      //add an attribute to the item that is dragged right now
      ui.attr('data-imgtaker', last_user);
      ui.addClass("user" + users.indexOf(last_user));
      return ui;
    },
    placeholder: 'ui-state-highlight',
    update: function (ev) {
      $(".ctviewer").each(function (index, element) {
        hideCTSlice(element);
      });
      makeAllImagesRightSize();
    },
    beforeStop: function (event, ui) {
      // check if ui.item parent has a class name of ctviewerImg
      if (ui.item.parent().hasClass('ctviewerImg')) {
        ui.item.remove();
      }
    }
  });

}

// Interact with MR
function startDragToMR(x, y, ws) {
  //return all draggable items, from x,y 
  let elements = document.elementsFromPoint(x, y);
  let draggableItems = elements.filter((element) => element.draggable);
  lastDraggedElementToMR = null;
  if (draggableItems.length > 0) {
    draggableItems.forEach((element) => {
      // check if element's data-transform exists and has value
      if (element.getAttribute("data-transform")) {
        //send data to MR
        lastDraggedElementToMR = element;
      }
    });
    console.log("Dragged element: ");

    if (lastDraggedElementToMR) {
      console.log(lastDraggedElementToMR);
      lastDraggedElementToMR.style.opacity = 0.3;
      //send ack to the MR that dragging was successful
      ws.send("draggedsuccess");
    }
  }
  else {
    lastDraggedElementToMR.style.opacity = 1;
    lastDraggedElementToMR = null;
    ws.send("draggedfail");
  }
}

function sendDataToMR(element, ws) {
  if (element) {
    switch (element.getAttribute("data-source")) {
      case "ct":
        if (element.getAttribute("data-filepath")) {
          ws.send("TransverseCSPlane,transform&frame," + element.getAttribute("data-transform")
            + "," + element.getAttribute("data-framenumber") + "," + element.getAttribute("data-filepath"));
        } else {
          ws.send("TransverseCSPlane,transform," + element.getAttribute("data-transform"));
        }
        break;
      case "camera":
        ws.send(base64ToBlob(element.src));
        if (element.getAttribute("data-filepath")) {
          ws.send("NewImagePlane,transform&frame," + element.getAttribute("data-transform")
            + "," + element.getAttribute("data-framenumber") + "," + element.getAttribute("data-filepath"));
        } else {
          ws.send("NewImagePlane,transform," + element.getAttribute("data-transform"));
        }
        break;
    }
  }
}

// Effects and Animations
function cursorHighlightEffect(element) {

  // If the current element is different from the previous element
  if (element != prev_hover_element) {
    // If there is a previous element, remove the highlight effect from it
    if (prev_hover_element && prev_hover_element.classList) {
      prev_hover_element.style = '';
    }

    // Add the highlight effect to the current element
    if (element.parentNode && element.parentNode.classList && element.parentNode.classList.contains('subContainer')) {
      //console.log(element.classList);
      element.style = 'background-color: #008cff';
      //console.log(element.classList);
      //console.log(element);
    }
    prev_hover_element = element;

    // Set the current element as the previous element
  }
}


function cursorFollows(e) {
  if (!e.isTrusted && typeof e.source !== 'undefined' && e.source === 'program') {
    document.getElementById('cursor').style.left = e.clientX + 'px';
    document.getElementById('cursor').style.top = e.clientY + 'px';
  }
}

function initializeMouseEvents() {
  document.addEventListener('mousemove', cursorFollows);
  document.addEventListener('mouseup', cursorFollows);
  document.addEventListener('mousedown', cursorFollows);

  document.getElementById("addNewDataContainer").addEventListener("mousedown", function (event) {
    //console.log("addNewDataContainer clicked");
    addNewSection();
  });
}

function emulateEvent(eventType, x = -1, y = -1) {
  //if (eventType !== "mousemove") console.log("emulateEvent: " + eventType + " " + x + " " + y);
  if (eventType == "mouseup") {
    draggedElement = null;
  }
  let dispatched = false;
  let event;

  if (x >= 0 && y >= 0 && x <= window.innerWidth && y <= window.innerHeight) {
    event = new MouseEvent(eventType, {
      bubbles: true,
      cancelable: true,
      view: window,
      clientX: x,  // x-coordinate of the mouse pointer, relative to the viewport
      clientY: y  // y-coordinate of the mouse pointer, relative to the viewport
    });

  } else {
    //console.log("There is no x,y in the range: " + x + " " + y);
    event = new MouseEvent(eventType, {
      bubbles: true,
      cancelable: true,
      view: window
    });
  }
  event.source = "program";
  if (draggedElement != null) {
    draggedElement.dispatchEvent(event);
    cursorHighlightEffect(draggedElement);
    dispatched = true;
  }

  let recyclebinareaHover = false;

  document.elementsFromPoint(x, y).forEach((element) => {
    if (!dispatched) {
      // For draggable objects
      if (element.attributes.getNamedItem("draggable") && element.attributes.getNamedItem("draggable").value === "true") {
        //console.log("dispatched to " + element.id + " " + element.className);
        element.dispatchEvent(event);
        dispatched = true;
        //effect:
        cursorHighlightEffect(element);
      }
      if (eventType === "mousedown" && element.tagName === "BUTTON") {
        element.dispatchEvent(event);
        dispatched = true;
      }
    }
    if (element.id === "recyclebinarea") {
      recyclebinareaHover = true;
      if (eventType === "mousemove") {
        event = new MouseEvent("mouseover", {
          bubbles: true,
          cancelable: true,
          view: window,
          clientX: x,  // x-coordinate of the mouse pointer, relative to the viewport
          clientY: y  // y-coordinate of the mouse pointer, relative to the viewport
        });
        event.source = "program";
        element.dispatchEvent(event);
        dispatched = true;
      } else if (eventType === "mouseup") {
        let event = new MouseEvent("mouseup", {
          bubbles: true,
          cancelable: true,
          view: window,
          clientX: x,  // x-coordinate of the mouse pointer, relative to the viewport
          clientY: y  // y-coordinate of the mouse pointer, relative to the viewport
        });
        event.source = "program";
        element.dispatchEvent(event);

        dispatched = true;
      }
    }
  });

  if (!recyclebinareaHover) {
    let recyclebinarea = document.getElementById("recyclebinarea");
    if (recyclebinarea != null) {
      let event_leave = new MouseEvent("mouseout", {
        bubbles: true,
        cancelable: true,
        view: window,
        clientX: x,  // x-coordinate of the mouse pointer, relative to the viewport
        clientY: y  // y-coordinate of the mouse pointer, relative to the viewport
      });
      event_leave.source = "program";
      recyclebinarea.dispatchEvent(event_leave);
    }
  }

  if (!dispatched) {
    document.dispatchEvent(event);
  }

  // select draggedElement
  if (eventType == "mousedown") {
    let selectDraggedElement = false;
    document.elementsFromPoint(x, y).forEach((element) => {
      if (!selectDraggedElement && element.attributes.getNamedItem("draggable") && element.attributes.getNamedItem("draggable").value === "true") {
        draggedElement = element;
        lastDraggedElement = element;
        selectDraggedElement = true;
      }
    });
  }
}

// Websocket Communication
function connectToTwoServers() {
  if (ws1 == null || (ws1.readyState !== WebSocket.OPEN && ws1.readyState !== WebSocket.CONNECTING)) {
    if (ws1 != null) ws1.close();
    ws1 = new WebSocket("ws://127.0.0.1:8080/Echo");
    connectToWebSocketServer(ws1);
  }
  if (ws2 == null || (ws2.readyState !== WebSocket.OPEN && ws2.readyState !== WebSocket.CONNECTING)) {
    if (ws2 != null) ws2.close();
    ws2 = new WebSocket("ws://192.168.10.172:8080/Echo");
    connectToWebSocketServer(ws2);
  }
  setTimeout(() => {
    connectToTwoServers();
  }, connectionWait);
}

function connectToWebSocketServer(ws) {
  ws.onopen = function (event) {
    console.log("WebSocket connection established");
    if (ws === ws1) {
      let state = document.getElementById("user1State");
      state.innerHTML = "User 1: Connected";
      state.style.color = "green";
    } else if (ws === ws2) {
      let state2 = document.getElementById("user2State");
      state2.innerHTML = "User 2: Connected";
      state2.style.color = "green";
    }
  };
  ws.onmessage = function (event) {
    let msg = event.data;

    // is type of msg a blob?
    if (msg instanceof Blob) {
      isPNG(msg).then((result) => {
        if (result[0]) {
          // if it's png, it's a CT slice
          //console.log("it's a png, user: " + last_user );
          showRealTimeCTSlice(result[1], result[2]);
        } else {
          // if it's not png, it's jpeg
          //console.log("it's a jpeg, user: " + last_user);
          addImage(result[1], result[2], "camera");
        }
      });
      return;
    }

    // determine the function
    let userFunc = extractUserFunc(msg);
    //check if userFunc.user exists in users list
    if (!users.includes(userFunc.user)) {
      users.push(userFunc.user);
    }
    // check if user changed
    if (userFunc.user !== last_user) {
      console.log("User changed from " + last_user + " to " + userFunc.user);
      last_user = userFunc.user;
    }

    switch (userFunc.func) {
      case "movecursor":
        let nums = extractFloatingPoints(msg.substring(userFunc.user.length + userFunc.func.length + 2));
        cursor_x = Number(nums[0]) * window.innerWidth;
        cursor_y = window.innerHeight - Number(nums[1]) * window.innerHeight;
        emulateEvent("mousemove", cursor_x, cursor_y);
        //startDragging(cursor_x, cursor_y, "right");
        isDrawingLine = true;
        break;
      case "echo":
        break;
      case "imagedata":
        break;
      case "leftmidpinchstart": case "rightmidpinchstart":
        startDragToMR(cursor_x, cursor_y, ws);
        break;
      case "leftmidpinchfinish": case "rightmidpinchfinish":
        if (lastDraggedElementToMR) lastDraggedElementToMR.style.opacity = 1;
        //endDragToMR(cursor_x, cursor_y);
        break;

      case "rightpinchstart": case "leftpinchstart":
        emulateEvent("mousedown", cursor_x, cursor_y);
        // startDragging(cursor_x, cursor_y, "right");
        isDrawingLine = true;
        break;
      case "rightpinchfinish": case "leftpinchfinish":
        emulateEvent("mouseup", cursor_x, cursor_y);
        // stopDragging();
        isDrawingLine = false;
        break;

      case "newwords":
        addWordsToCurrentSection(userFunc.user, msg.substring(userFunc.user.length + userFunc.func.length + 4));
        break;
      case "newsection":
        break;
      case "transform":
        //console.log("new Transform received: " + msg.substring(userFunc.user.length + userFunc.func.length + 4));
        transform = msg.substring(userFunc.user.length + userFunc.func.length + 4);
        break;
      case "transform&frame": // working here
        let res = extractTranFrameFile(msg.substring(userFunc.user.length + userFunc.func.length + 4));
        // frameNumber = 
        frameNumber = res.frameNum;
        // transform = transformandframe.substring(0, transformandframe.length - frameNumber.length - 1);
        transform = res.transform;
        filePath = res.filePath;
        break;
      case "retByteData":
        break;
      case "retDraggedElement":
        sendDataToMR(lastDraggedElementToMR, ws);
        lastDraggedElementToMR.style.opacity = 1;
        lastDraggedElementToMR = null;
        break;
      case "joinRequest":
        showQRCode();
        break;
    }
  };
  ws.onerror = function (event) {
    console.log("WebSocket error: " + event.message);
    if (ws === ws1) {
      let state = document.getElementById("user1State");
      state.innerHTML = "User 1: Not Connected";
      state.style.color = "red";
    } else if (ws === ws2) {
      let state2 = document.getElementById("user2State");
      state2.innerHTML = "User 2: Not Connected";
      state2.style.color = "red";
    }
  };
  ws.onclose = function (event) {
    console.log("WebSocket connection closed");
    if (ws === ws1) {
      let state = document.getElementById("user1State");
      state.innerHTML = "User 1: Not Connected";
      state.style.color = "red";
    } else if (ws === ws2) {
      let state2 = document.getElementById("user2State");
      state2.innerHTML = "User 2: Not Connected";
      state2.style.color = "red";
    }
  };
}

// Graphics
function drawLine(x, y) {
  if (!isDrawingLine) return;
  ctx.strokeStyle = 'red';
  ctx.lineWidth = 10;
  ctx.beginPath();
  ctx.moveTo(prev_cursor_x, prev_cursor_y); // Move the pen to the position of the first cursor
  ctx.lineTo(x, y); // Draw a line to the position of the second cursor
  ctx.stroke(); // Stroke the path to draw the line
  prev_cursor_x = x;
  prev_cursor_y = y;
}
function showQRCode() {
  let qrcode = document.querySelector(".qrcodeContainer>img");
  qrcode.parentNode.style.display = "block";
  qrcode.style.position = "fixed";
  qrcode.height = window.innerHeight / 2;
  qrcode.width = window.innerHeight / 2;

  qrcode.style.left = (window.innerWidth - qrcode.width) / 2 + "px";
  qrcode.style.top = (window.innerHeight - qrcode.height) / 2 + "px";

  setTimeout(() => {
    hideQRCode();
  }, qrcodeTimeout);
}
function hideQRCode() {
  document.querySelector(".qrcodeContainer").style.display = "none";
}
//Helper Functions

function extractUserFromBolbData(data) {
  let index = -1;
  let endOfSeq = Math.min(data.byteLength - 4, 100); //check maximum 100 first bytes
  var arr = new Uint8Array(data);
  for (let i = 0; i <= endOfSeq; i++) {
    // console.log(arr[i] + "," + arr[i + 1]);
    if (arr[i] === 13 && arr[i + 1] === 10 && arr[i + 2] === 13 && arr[i + 3] === 10) {
      index = i;
      break;
    }
  }
  if (index !== -1) {
    return String.fromCharCode.apply(String, arr.slice(0,index));
  } else {
    return null;
  }
}


function extractUserFunc(str) {
  let result = {};
  let parts = str.split('$');
  result.user = parts[1];
  parts = parts[2].split('%');
  result.func = parts[1];
  return result;
}

function extractFloatingPoints(str) {
  // Use a regular expression to match all digits in the string
  var matches = str.match(/[\-\d.]+/g);
  if (matches) {
    // Convert the matches to numbers and return them in an array
    let nums = matches.map(Number);
    return Array.of(nums[0], nums[2]);
  } else {
    // Return an empty array if no matches were found
    return [];
  }
}

// Extracts the transformation, frame number, and file path from the string
function extractTranFrameFile(str) {
  let result = {};
  let parts = str.split(',');
  //merge the the first 10 parts of the array into one string
  result.transform = parts.slice(0, 10).join(',');
  result.frameNum = parts[10];
  // merge all remaining parts
  result.filePath = parts.slice(11).join(',');
  return result;
}

function encode(input) {
  let keyStr = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
  let output = "";
  let chr1, chr2, chr3, enc1, enc2, enc3, enc4;
  let i = 0;

  while (i < input.size) {
    chr1 = input[i++];
    chr2 = i < input.size ? input[i++] : Number.NaN;
    chr3 = i < input.size ? input[i++] : Number.NaN;

    enc1 = chr1 >> 2;
    enc2 = ((chr1 & 3) << 4) | (chr2 >> 4);
    enc3 = ((chr2 & 15) << 2) | (chr3 >> 6);
    enc4 = chr3 & 63;

    if (isNaN(chr2)) {
      enc3 = enc4 = 64;
    } else if (isNaN(chr3)) {
      enc4 = 64;
    }
    output += keyStr.charAt(enc1) + keyStr.charAt(enc2) + keyStr.charAt(enc3) + keyStr.charAt(enc4);
  }
  return output;
}

function blobToBase64(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => {
      resolve(reader.result);
    };
    reader.onerror = reject;
    reader.readAsDataURL(blob);
  });
}
function base64ToBlob(base64String) {
  const parts = base64String.split(';base64,');
  const contentType = parts[0].split(':')[1];
  const raw = window.atob(parts[1]);
  const rawLength = raw.length;
  const uInt8Array = new Uint8Array(rawLength);
  for (let i = 0; i < rawLength; ++i) {
    uInt8Array[i] = raw.charCodeAt(i);
  }
  return new Blob([uInt8Array], { type: contentType });
}
// function base64ToUint8Array(base64) {
//   const binary = atob(base64);
//   const len = binary.length;
//   const bytes = new Uint8Array(len);
//   for (let i = 0; i < len; i++)
//       bytes[i] = binary.charCodeAt(i);
//   return bytes;
// }

const isPNG = (blob) => {
  const reader = new FileReader();
  reader.readAsArrayBuffer(blob);

  return new Promise((resolve) => {
    reader.onloadend = () => {
      //check if username is embedded in the first bytes
      let extractedUser = extractUserFromBolbData(reader.result);
      let offset = 0;
      
      if(extractedUser){
        offset = extractedUser.length + 4;
        last_user = extractedUser;
      }
      
      // Get the first eight bytes of the file
      const uint = new Uint8Array(reader.result.slice(offset, offset+8));
      
      // Check if the file starts with the PNG header
      const signature = String.fromCharCode(...uint);
      if (signature === '\x89PNG\r\n\x1a\n') {
        resolve([true, extractedUser, reader.result.slice(offset)]);
      } else {
        resolve([false, extractedUser, reader.result.slice(offset)]);
      }
    };
  });
};

const isJPG = (blob) => {
  // Create a new FileReader
  const reader = new FileReader();
  // Create a promise that resolves when the file has been read
  return new Promise((resolve) => {
    reader.onloadend = () => {
      // Get the first two bytes of the file
      const uint = new Uint8Array(reader.result.slice(0, 2));
      // Check if the file starts with the JFIF or Exif header
      const signature = String.fromCharCode(...uint);
      if (signature === '\xff\xd8') {
        resolve(true);
      } else {
        resolve(false);
      }
    };
  });
};