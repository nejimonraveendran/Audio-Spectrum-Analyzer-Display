#include "Common.h"

//web page for the spectrum analyzer display.
//if you want to edit this, you can use the index.html page included in the source code and copy it to this variable.
char g_webPage[] PROGMEM = R"=====( 

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Spectrum Analyzer Display</title>
      
    <script src="https://code.jquery.com/jquery-3.7.1.min.js" integrity="sha256-/JqT3SQfawRcv/BIHPThkBvs0OEvtFFmqPF/lYI/Cxo=" crossorigin="anonymous"></script>
    
    <style>
        h1{
            font-family: Arial, Helvetica, sans-serif;
        }

        label{
            font-family: 'Courier New', Courier, monospace;
        }

        body{
            background-color: rgb(17, 18, 19);
            color:aliceblue;
        }

        .error{
            color: lightsalmon;
        }

        .container{
            margin:30px;
        }

        .rowColor{
            cursor: pointer;
        }

        .colColor{
            cursor: pointer;
        }

        .gradientTop{
            cursor: pointer;
        }

        .gradientBottom{
            cursor: pointer;
        }

        .pixelWrapper {
          width: 30px;
          height: 30px;
          overflow: hidden;
          border-radius: 50%;
          cursor: pointer;
        }

        .pixelWrapperMargin {
          margin-right: auto;
          margin-left: auto;
        }
    
        .pixel {
          width: 100px;
          height: 100px;
          top: 50%;
          left: 50%;
          transform: translate(-50%, -50%);
          overflow: hidden;
          border: none;
          cursor: pointer;
        }

        .slider{
            width: 50%;
        }

        td{
            padding-right: 25px;
            padding-bottom: 15px;
            margin-left: auto;
            margin-right: auto;
        }

        #btnDeploy{
            width: 150px;
            height: 70px;
            background-color: rgb(3, 99, 141);
            font-weight: bolder;
            color: aliceblue;
            cursor: pointer;
            
        }

        #globalColor{
            width: 100%;
            cursor: pointer;
        }

      </style>
</head>
<body>
    <h1>Spectrum Analyzer Display</h1>

    <hr>
    <div id="colorPickerContainer"></div>
    <div class="container hidden" id="container">
        <label>Peak wait (millisecs)</label>
        <div>
            <input class="slider" type="range" min="1" max="50000" step="10" value="1500" id="sldPeakDelay" oninput="peakDelayChanged();">
            <span id="spanPeakDelay"></span>
        </div>
        <br/><br/>
    
        <label>Peak falldown (millisecs)</label>
        <div>
            <input class="slider" type="range" min="1" max="250" step="1" value="25" id="sldPeakSpeed" oninput="peakSpeedChanged();"> 
            <span id="spanPeakSpeed"></span>
        </div>
        <br/><br/>

        <label>Speed filter</label>
        <div>
            <input class="slider" type="range" min="0" max="200" step="1" value="1" id="sldSpeedFilter" oninput="speedFilterChanged();">
            <span id="spanSpeedFilter"></span>
        </div>
        <br/><br/>

        <label>Brightness</label>
        <div>
            <input class="slider" type="range" min="0" max="100" step="1" value="20" id="sldBrightness" oninput="brightnessChanged();">
            <span id="spanBrightness"></span>
        </div>
        <br/><br/>

        <label>Level Attenuation</label>
        <div>
            <input class="slider" type="range" min="10000" max="500000" step="1000" value="20" id="sldAttenuation" oninput="attenuationChanged();">
            <span id="spanAttenuation"></span>
        </div>
        <br/><br/>


        <label>Peak color</label>
        <div class="pixelWrapper">
          <input id="peakPixel" class="pixel" type="color" value="#ffffff"/>
        </div>
    
        <br/><br/>
        
        <label>Matrix color</label>
        <table id="tblMatrix">
          <tbody>
            
          </tbody>
        </table>
    
        <br/><br/>
        
        <button id="btnDeploy" onclick="deploy();">Deploy</button>    
    </div>
          
    <script>
        const _baseUrl = 'http://10.0.0.64'; //window.location.origin;  
        const _localStorageConfigKey = 'state';
        var _noOfRows = 0;  
        var _noOfCols = 0;  

        $(document).ready(function(){
            get("/config", function(res){ //get fom server.
                if(res.status === 'success'){
                    buildUI(res.response);
                }else{
                    $('#container').html('<span class="error">Unable to reach Spectrum Analyzer Server APIs!</span>');
                }                
            });
        });


        function buildUI(data){
            _noOfCols = data.noOfCols;
            _noOfRows = data.noOfRows;

            buildMatrix();
            
            //if local config exists, retrieve it and update local UI as well as the server
            try{
                const state = localStorage.getItem(_localStorageConfigKey);
                const objState = JSON.parse(state);
                updateUI(objState);      
                deploy();  //deploy to server
            }catch (error){ //if unable to retrieve/parse local state object, use the values from the server and then sync with server.
                updateUI(data);
                deploy();
            }
        }
        
        function updateUI(objState){
            //set peak delay
            $('#sldPeakDelay').val(objState.peakDelay);
            peakDelayChanged();

            //set peak speed
            $('#sldPeakSpeed').val(objState.peakSpeed);
            peakSpeedChanged();
            
            //set speed filter
            $('#sldSpeedFilter').val(objState.speedFilter * 1000);
            speedFilterChanged();

            //set brightness
            $('#sldBrightness').val(objState.brightness);
            brightnessChanged();

            //set attenuation
            $('#sldAttenuation').val(invertAttenuationValue(objState.atten));
            attenuationChanged()
            
            //set peak pixel
            $('#peakPixel').val(RGBjsonToString(JSON.stringify(objState.peak)));

            //set matrix pixels
            for (let i = 0; i < objState.pixels.length; i++) {
                let displayPixel = $(`#tblMatrix tbody tr td div input[data-idx=${i}].pixel`); 
                let clr = RGBjsonToString(JSON.stringify(objState.pixels[i]));
                //console.log(clr);
                $(displayPixel).val(clr);
            }
        }

        function speedFilterChanged(){
            let speedFilter = $('#sldSpeedFilter').val() / 1000;
             $('#spanSpeedFilter').html(speedFilter);
        }

        function peakDelayChanged(){
            $('#spanPeakDelay').html($('#sldPeakDelay').val());
        }
            
        function peakSpeedChanged(){
            $('#spanPeakSpeed').html($('#sldPeakSpeed').val());
        }

        function brightnessChanged(){
            $('#spanBrightness').html($('#sldBrightness').val());
        }

        function attenuationChanged(){
            let attenVal = parseInt($('#sldAttenuation').val());
            $('#spanAttenuation').html(invertAttenuationValue(attenVal));
        }

        function invertAttenuationValue(value){
            let attenMin = parseInt($('#sldAttenuation').attr('min'));
            let attenMax = parseInt($('#sldAttenuation').attr('max'));
            return invertValue(attenMin, attenMax, value); 
        }

        function globalColorChanged(ctl){
            let selectedColor = $(ctl).val();
            const allPixels = $(`#tblMatrix tbody tr td div input.pixel`); 
            for (let i = 0; i < allPixels.length; i++) {
                $(allPixels[i]).val(selectedColor);
            }            
        }

        function colColorChanged(ctl){
            let selectedColor = $(ctl).val();
            let x = $(ctl).attr("data-x");
            let colPixels = $(`#tblMatrix tbody tr td div input[data-x=${x}].pixel`); 

            for (let y = 0; y < colPixels.length; y++) {
                $(colPixels[y]).val(selectedColor);//update locally
            }
        }

        function rowColorChanged(ctl){
            let selectedColor = $(ctl).val();
            let y = $(ctl).attr("data-y");
            let rowPixels = $(ctl).closest('tr').find('input.pixel');
            
            for (let x = 0; x < rowPixels.length; x++) {
                $(rowPixels[x]).val(selectedColor);//update locally
            }
        }


        function gradientChanged(ctl){
            let topColor =  $('#gradientTop').val();
            let bottomColor = $('#gradientBottom').val();

            let gradient = generateGradient(topColor, bottomColor, _noOfRows); //generate gradient colors

            for (let x = 0; x < _noOfCols; x++) {
                let colPixels = $(`#tblMatrix tbody tr td div input[data-x=${x}].pixel`); 

                for (let y = 0; y < _noOfRows; y++) {
                    let pixel = $(`#tblMatrix tbody tr td div input[data-x=${x}][data-y=${y}].pixel`); 
                    $(pixel).val(gradient[y]);
                }
            }
        }


        function generateGradient(startColor, endColor, steps) {
            const startRgb = hexToRgb(startColor);
            const endRgb = hexToRgb(endColor);

            const gradient = [];

            for (let i = 0; i < steps; i++) {
                const t = i / (steps - 1); // Interpolation factor
                const r = Math.round(startRgb.r + t * (endRgb.r - startRgb.r));
                const g = Math.round(startRgb.g + t * (endRgb.g - startRgb.g));
                const b = Math.round(startRgb.b + t * (endRgb.b - startRgb.b));
                gradient.push(rgbToHex(r, g, b));
            }

            return gradient;
        }

        function hexToRgb(hex) {
            // Convert HEX to RGB
            const bigint = parseInt(hex.slice(1), 16);
            return {
                r: (bigint >> 16) & 255,
                g: (bigint >> 8) & 255,
                b: bigint & 255
            };
        }

        function rgbToHex(r, g, b) {
            // Convert RGB to HEX
            return `#${((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1).toUpperCase()}`;
        }
        

        function post(path, json, cb){
            $.ajax({
                type: 'post',
                url: _baseUrl + path + "?data=" + json,
                contentType: "x-www-form-urlencoded",
                success: function(res){
                    cb({status: 'success', response: res});
                },
                error: function(err){
                    cb({status: 'fail', response: err});
                }
            });
        }    
        
        function get(path, cb){
            $.ajax({
                type: 'get',
                url: _baseUrl + path,
                success: function(res){
                    cb({status: 'success', response: res});
                },
                error: function(err){
                    cb({status: 'fail', response: res});                    
                }
            });

        }    
        
        function RGBstringToJson(colorString) { //#ffffff to {"r": 255, "g": 255, "b": 255}
          const r = parseInt(colorString.substr(1, 2), 16)
          const g = parseInt(colorString.substr(3, 2), 16)
          const b = parseInt(colorString.substr(5, 2), 16)
          
          return `{"r":${r}, "g":${g}, "b":${b}}`;
        }


        function RGBjsonToString(colorJson) { // {"r": 255, "g": 255, "b": 255} to #ffffff to
            let objColor = JSON.parse(colorJson);    
            let r = '';
            let g = '';
            let b = '';

            r = objColor.r < 16 ? '0' + objColor.r.toString(16) : objColor.r.toString(16);
            g = objColor.g < 16 ? '0' + objColor.g.toString(16) : objColor.g.toString(16);
            b = objColor.b < 16 ? '0' + objColor.b.toString(16) : objColor.b.toString(16);
            
            const colorString = "#" +  r + "" + g + "" + b;  
            return colorString;
        }

        function buildMatrix(){
            //build global color picker
            let row = '<tr>';
            row+='<td></td>';
            row+=`<td colspan="${_noOfCols}">`;
            row+='<input id="globalColor" type="color" value="#ffffff" onchange="globalColorChanged(this);"/>';
            row+='</td>';
            row+='</tr>';
            $('#tblMatrix tbody').append(row);

            //build row color pickers (at the top)
            row = '<tr>';
            for(let x=0;x<_noOfCols+1;x++){ //build col color pickers
                row+='<td>';
                
                if(x>0){ 
                    let xx = x-1;
                    row+='<div>';
                    row+='<input id="colColor_' + xx + '" class="colColor" type="color" value="#ffffff" data-x="' + xx + '" onchange="colColorChanged(this);"/>';
                    row+='</div>'
                }
                
                row+='</td>';
            }
            row+='</tr>';
            $('#tblMatrix tbody').append(row);


            for(let y=0;y<_noOfRows;y++){
                //build row color pickers (left side)
                row = '<tr>';
                row+='<td>';
                row+='<div>';
                row+='<input id="rowColor_' + y + '" class="rowColor" type="color" value="#ffffff" data-y="' + y + '" onchange="rowColorChanged(this);"/>';
                row+='</div>'
                row+='</td>';
                
                //build matrix pixel color pickers.
                for(let x=0;x<_noOfCols;x++){
                    let r = (_noOfRows-1)-y;
                    let idx = x * _noOfRows + r;
                    row+='<td>';
                    row+='<div class="pixelWrapper pixelWrapperMargin">';
                    row+='<input id="matrixPixel_' + idx + '" class="pixel" type="color" value="#000000" data-x="' + x + '" data-y="' + y + '" data-idx="' + idx + '"/>';
                    row+='</div>'
                    row+='</td>';
                }
                
                //build top gradient
                if(y==0){
                    row+='<td rowspan="18" class="gradientCell">';
                    row+='<div>';
                    row+='<input id="gradientTop" class="gradientTop" type="color" value="#ffffff" onchange="gradientChanged(this);"/>';
                    row+='</div>'
                    row+='<div>';
                    row+='<input id="gradientBottom" class="gradientBottom" type="color" value="#ffffff" onchange="gradientChanged(this);"/>';
                    row+='</div>'
                    row+='</td>';
                }

                row+='</tr>';
                $('#tblMatrix tbody').append(row);
            }

            $('#gradientTop').css('height', $('#gradientTop').parent('div').parent('td').height());
            $('#gradientBottom').css('height', $('#gradientBottom').parent('div').parent('td').height());
            
        }

        function deploy(){            
            let state = buildState();
            const payload = JSON.stringify(state);

            localStorage.setItem(_localStorageConfigKey, payload);
            
            post("/deploy", payload, function(res){ //update server.
                //success
                localStorage.setItem(_localStorageConfigKey, payload); //update local storage
                updateUI(state);
            });
        }


        function buildState(){
            let state = {
                peakDelay: 125,
                peakSpeed: 25,
                speedFilter: 0.7,
                peak: {},
                pixels: [],
            };

            //set properties
            state.peakDelay =  parseInt($('#sldPeakDelay').val());
            state.peakSpeed =  parseInt($('#sldPeakSpeed').val());
            state.speedFilter  = $('#sldSpeedFilter').val() / 1000;
            state.brightness  = $('#sldBrightness').val();
            state.atten =  invertAttenuationValue(parseInt($('#sldAttenuation').val()));
            state.peak = JSON.parse(RGBstringToJson($('#peakPixel').val()));

            //populate matrix pixel data.
            const allPixels = $(`#tblMatrix tbody tr td div input.pixel`); 
            for (let i = 0; i < allPixels.length; i++) {
                const pixel = allPixels[i];
                const jsonColor = RGBstringToJson($(pixel).val());
                const objColor = JSON.parse(jsonColor);
                const idx = parseInt($(pixel).attr('data-idx'));    
                const x = parseInt($(pixel).attr('data-x'));
                const y = parseInt($(pixel).attr('data-y'));
                
                state.pixels.push({
                    x: x,
                    y: y,
                    i: idx,
                    r: objColor.r,
                    g: objColor.g,
                    b: objColor.b
                });
            }            
            state.pixels.sort((a,b) => a.i < b.i ? -1 : 1); //sort in the order of index.
            return state;
        }

        function invertValue(min, max, value){
            return (min + max) - value;
        }
    </script>
</body>
</html>

)=====";  //do not remove this line