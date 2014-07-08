/**
* @overview OCM charging location browser/editor Mobile App
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com

http://openchargemap.org
*/

/*typescript*/
/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />

/// <reference path="OCM_Data.ts" />

////////////////////////////////////////////////////////////////

module OCM {

    export class FileUpload extends OCM.Base {
        private img: HTMLImageElement;
        private ctx: CanvasRenderingContext2D;
        private canvas: HTMLCanvasElement;

        constructor() {
            super();
            this.initFileUpload();
        }

        checkFileSelection() {
            var fileUploadElement = <any>document.getElementById("fileupload");
            var file = fileUploadElement.files[0];

            var reader = new FileReader();
            reader.onload = function (e) {
                $(".file-upload-input-title").text("Change");
                $(".file-upload-clear").show();
                $(".file-upload-filename").val(file.name);
            };

            reader.readAsDataURL(file);

            var maxSizeBytes = 1024 * 1024 * 5;
            var fileSizeMB = file.size / 1024 / 1024;
            var maxSizeMB = maxSizeBytes / 1024 / 1024;

            if (file.size > maxSizeBytes) {
                alert("The file you have selected is too big for upload (" + Math.round(fileSizeMB * 100) / 100 + "MB, max size is " + maxSizeMB + "MB).");
            } else {
                $("#file-info").html("Selected file is " + Math.round(fileSizeMB * 100) / 100 + "MB in size.");

                //this.preprocessImageUpload(file);
            }
        }

        rotatePreview(angle,x,y) {
            var TO_RADIANS = Math.PI / 180;
            var context = this.ctx;
            var image = this.img;            
               
            context.save();
            context.translate(x, y);
            context.rotate(angle * TO_RADIANS);                
            context.drawImage(image, -(image.width / 2), -(image.height / 2));
            context.restore();
            
        }

        preprocessImageUpload(file) {
            var app = this;

            //http://hacks.mozilla.org/2011/01/how-to-develop-a-html5-image-uploader/
            this.img = document.createElement("img");
            var reader = new FileReader();
            reader.onload = function (e) {
                app.img.src = e.target.result;

                app.canvas = <HTMLCanvasElement>document.getElementById("file-upload-preview");
                
                var MAX_WIDTH = 280;
                var MAX_HEIGHT = 100;
                var width = app.img.width;
                var height = app.img.height;

                if (width > height) {
                    if (width > MAX_WIDTH) {
                        height *= MAX_WIDTH / width;
                        width = MAX_WIDTH;
                    }
                } else {
                    if (height > MAX_HEIGHT) {
                        width *= MAX_HEIGHT / height;
                        height = MAX_HEIGHT;
                    }
                }
                app.canvas .width = width;
                app.canvas .height = height;
                app.ctx = app.canvas .getContext("2d");
                app.ctx.drawImage(app.img, 0, 0, width, height);

                //rotate
                /*canvas.width = height;
                canvas.height = width;
                app.rotatePreview(90, width / 2, height / 2);*/
            }
            reader.readAsDataURL(file);

           
        }

        getImageData() {
            var dataurl = this.canvas.toDataURL("image/png");
            return dataurl;
        }

        initFileUpload() {
            //adapted from http://bootsnipp.com/snippets/featured/input-file-popover-preview-image
            var fileUploader = this;

            $(".file-upload-input input:file").change(function () {
                fileUploader.checkFileSelection();
            });

            // Set the clear onclick function
            $('.file-upload-clear').click(function () {
                $('.file-upload-filename').val("");
                $('.file-upload-clear').hide();
                $('.file-upload-input input:file').val("");
                $(".file-upload-input-title").text("Browse");
            });
        }
    }
}