using System;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;
using System.Buffers;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;

namespace JayTom.Dws.Ocr.ExpressBill {

    public class ExpressBillPool {
        private readonly DefaultObjectPool<ExpressBill> _objectPool;

        public ExpressBillPool(int maxObjects) {
            _objectPool = new DefaultObjectPool<ExpressBill>(
                new ExpressBillPooledObjectPolicy(this), maxObjects);
        }

        public ExpressBill GetObject() {
            return _objectPool.Get();
        }

        public void ReturnObject(ExpressBill expressBill) {
            _objectPool.Return(expressBill);
        }
    }

    public class ExpressBillPooledObjectPolicy : IPooledObjectPolicy<ExpressBill> {
        private readonly ExpressBillPool _pool;

        public ExpressBillPooledObjectPolicy(ExpressBillPool pool) {
            _pool = pool;
        }

        public ExpressBill Create() {
            return new ExpressBill(_pool);
        }

        public bool Return(ExpressBill obj) {
            // Optionally perform any cleanup or reset logic before returning the object to the pool
            return true;
        }
    }
}
