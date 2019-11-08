/**
* @overview OCM charging location browser/editor Mobile App
* @author Christopher Cook
* @copyright Webprofusion Ltd http://webprofusion.com

http://openchargemap.org
*/
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
/*typescript*/
/// <reference path="TypeScriptReferences/jquery/jquery.d.ts" />
/// <reference path="TypeScriptReferences/phonegap/phonegap.d.ts" />
/// <reference path="OCM_Data.ts" />
////////////////////////////////////////////////////////////////
var OCM;
(function (OCM) {
    var FileUpload = /** @class */ (function (_super) {
        __extends(FileUpload, _super);
        function FileUpload() {
            var _this = _super.call(this) || this;
            _this.initFileUpload();
            return _this;
        }
        FileUpload.prototype.checkFileSelection = function () {
            var fileUploadElement = document.getElementById("fileupload");
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
            }
            else {
                $("#file-info").html("Selected file is " + Math.round(fileSizeMB * 100) / 100 + "MB in size.");
                //this.preprocessImageUpload(file);
            }
        };
        FileUpload.prototype.rotatePreview = function (angle, x, y) {
            var TO_RADIANS = Math.PI / 180;
            var context = this.ctx;
            var image = this.img;
            context.save();
            context.translate(x, y);
            context.rotate(angle * TO_RADIANS);
            context.drawImage(image, -(image.width / 2), -(image.height / 2));
            context.restore();
        };
        FileUpload.prototype.preprocessImageUpload = function (file) {
            var app = this;
            //http://hacks.mozilla.org/2011/01/how-to-develop-a-html5-image-uploader/
            this.img = document.createElement("img");
            var reader = new FileReader();
            reader.onload = function (e) {
                app.img.src = e.target.result;
                app.canvas = document.getElementById("file-upload-preview");
                var MAX_WIDTH = 280;
                var MAX_HEIGHT = 100;
                var width = app.img.width;
                var height = app.img.height;
                if (width > height) {
                    if (width > MAX_WIDTH) {
                        height *= MAX_WIDTH / width;
                        width = MAX_WIDTH;
                    }
                }
                else {
                    if (height > MAX_HEIGHT) {
                        width *= MAX_HEIGHT / height;
                        height = MAX_HEIGHT;
                    }
                }
                app.canvas.width = width;
                app.canvas.height = height;
                app.ctx = app.canvas.getContext("2d");
                app.ctx.drawImage(app.img, 0, 0, width, height);
                //rotate
                /*canvas.width = height;
                canvas.height = width;
                app.rotatePreview(90, width / 2, height / 2);*/
            };
            reader.readAsDataURL(file);
        };
        FileUpload.prototype.getImageData = function () {
            var dataurl = this.canvas.toDataURL("image/png");
            return dataurl;
        };
        FileUpload.prototype.initFileUpload = function () {
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
        };
        return FileUpload;
    }(OCM.Base));
    OCM.FileUpload = FileUpload;
})(OCM || (OCM = {}));
//# sourceMappingURL=OCM_FileUpload.js.map