using Microsoft.AspNetCore.Mvc;

namespace JayTom.Dws.CloudApi.Vo {

    public class JsonResultVo : JsonResult {

        public JsonResultVo(object value) : base(value) {
        }

        public JsonResultVo(object value, object serializerSettings) : base(value, serializerSettings) {
        }

        public static JsonResult Fail(string msg, int statusCode = 200) {
            return new JsonResult(new { Result = false, Msg = msg }) { StatusCode = statusCode };
        }

        public static JsonResult Fail(string msg, object data, int statusCode = 200) {
            return new JsonResult(new { Result = false, Data = data, Msg = msg }) { StatusCode = statusCode };
        }

        public static JsonResult Success(string msg) {
            return new JsonResult(new { Result = true, Msg = msg }) { StatusCode = 200 };
        }

        public static JsonResult Success(string msg, object data) {
            return new JsonResult(new { Result = true, Data = data, Msg = msg }) { StatusCode = 200 };
        }

        public static JsonResult Success(string msg, int total, object data) {
            return new JsonResult(new { Result = true, Data = data, Total = total, Msg = msg }) { StatusCode = 200 };
        }
    }
}