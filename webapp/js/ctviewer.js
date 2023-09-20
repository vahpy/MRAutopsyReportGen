


var ctViewerBlocks = [];
var ctViewersTimer = [];
var ctViewerTimerInterval = [];
var minCTViewerTimerInterval = 3000;

//constants
const CTVIEWER_WIDTH = 560;

function addNewCTViewer(userName) {
  let ctViewerBlock = document.createElement('div');
  ctViewerBlock.id = 'ctviewer' + ctViewerBlocks.length + 1;
  ctViewerBlock.className = 'ctviewer';
  ctViewerBlock.setAttribute("data-user", userName);
  ctViewerBlock.setAttribute("data-framenumber", frameNumber);
  ctViewerBlock.setAttribute("data-filepath", filePath);
  ctViewerBlock.style.left = (ctViewerBlocks.length * CTVIEWER_WIDTH) + "px";
  ctViewerBlock.innerHTML = '<label class="userCTViewerID">' + userName.charAt(0).toUpperCase() + userName.slice(1) + '</label>\
  <button id="ctviewerToggleBtn" class="addbutton ctviewertogglebtn" style="background-color: orange;left:10px;position: relative;"\
  onmousedown="toggleCTSlice(this)" z-index="1000">↑ Show CT Slice ↑</button>\
  <div class="ctviewerImg"><img draggable="true" src="qrcode2.png" alt="ctviewer" style="display: none;" z-index="2000"></div>';
  ctViewerBlocks.push(ctViewerBlock);
  document.getElementById('ctViewersContainer').appendChild(ctViewerBlock);
  showCTSlice(ctViewerBlock);
  ctViewerTimerInterval[ctViewerBlock.id] = minCTViewerTimerInterval;
  resetAutoHideCTSliceTimer(ctViewerBlock);
  
  makeAllContainersSortable();
  //makeCTViewerImgDraggable(ctViewerBlock);
  return ctViewerBlock;
}


function makeCTViewDraggable(){
  const containers = document.querySelectorAll('.subContainer');
  
  if(containers.length === 0) return;
  
  const droppable = new Draggable.Droppable(containers, {
    draggable: 'img',
    dropzone: '.subContainer',
    mirror: {
      constrainDimensions: true,
    },
  });

  let droppableOrigin;


  return droppable;
}

function showRealTimeCTSlice(userName, imageData) {
  const blob = new Blob([imageData], { type: 'image/png' });
  blobToBase64(blob).then(base64 => {
    // show 
    let element = document.querySelector("[data-user='" + userName + "']");
    if (!element) {
      //console.log("Show real time CT slice for user: " + userName);
      element = addNewCTViewer(userName);
    }
    let imgElement = element.getElementsByTagName("img")[0];
    imgElement.src = base64;
    imgElement.setAttribute("data-transform", transform);
    imgElement.setAttribute("data-framenumber", frameNumber);
    imgElement.setAttribute("data-filepath", filePath);
    imgElement.setAttribute("data-source", "ct");
    showCTSlice(element);
    resetAutoHideCTSliceTimer(element);
  });
}

// function addCTSlice(user, imageData) {
//   addImage(user, imageData, 'image/png');
// }

function saveCTSlice(ctviewerowner, button) {
  let ctViewer = button.parentNode;
  let ctImage = document.querySelector("#" + ctViewer.id + " img");
  //get img from ctviewer div
  let data = ctImage.src;
  let transform = null;
  if (ctImage.getAttribute("data-transform") != null && ctImage.getAttribute("data-transform") != undefined) {
    transform = ctImage.getAttribute("data-transform");
  }
  //if last_user (global variable) is not null and defined, use it as the taker
  if (last_user == undefined || last_user == null) {
    addImageByBase64(ctviewerowner, data, "ct", transform);
  } else {
    addImageByBase64(last_user, data, "ct", transform);
  }
  makeAllContainersSortable();
}

function toggleCTSlice(button) {
  let ctViewer = button.parentNode;
  let ctImage = document.querySelector("#" + ctViewer.id + " img");

  if (ctImage.style.display == "none") {
    ctImage.style.display = "block";
    button.innerHTML = "↓ Hide CT Slice ↓";
    ctViewer.style.height = ctImage.offsetHeight + 70 + "px";
    ctViewerTimerInterval[ctViewer.id] = 1000000;
  }
  else {
    ctImage.style.display = "none";
    button.innerHTML = "↑ Show CT Slice ↑";
    ctViewer.style.height = "70px";
    ctViewerTimerInterval[ctViewer.id] = minCTViewerTimerInterval;
  }
}

function showCTSlice(ctViewer) {
  let ctImage = document.querySelector("#" + ctViewer.id + " img");
  let button = document.querySelector("#" + ctViewer.id + ">#ctviewerToggleBtn");
  if (ctImage.style.display == "block") return;
  ctImage.style.display = "block";
  button.innerHTML = "↓ Hide CT Slice ↓";
  ctViewer.style.height = ctImage.offsetHeight + 70 + "px";
}

function hideCTSlice(ctViewer) {
  let ctImage = document.querySelector("#" + ctViewer.id + " img");
  let button = document.querySelector("#" + ctViewer.id + ">#ctviewerToggleBtn");
  ctImage.style.display = "none";
  button.innerHTML = "↑ Show CT Slice ↑";
  ctViewer.style.height = "70px";
}

function resetAutoHideCTSliceTimer(ctViewer) {
  clearTimeout(ctViewersTimer[ctViewer.id]);
  ctViewersTimer[ctViewer.id] = setTimeout(function () {
    hideCTSlice(ctViewer);
  }, ctViewerTimerInterval[ctViewer.id]);
}

function makeCTViewerImgDraggable(ctViewer) {
  let ctImage = document.querySelector("#" + ctViewer.id + " img");
  ctImage.addEventListener('dragstart', function (event) {
    event.dataTransfer.setData('text/plain', null);
  });
}