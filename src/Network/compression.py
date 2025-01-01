import zlib
import lzma
import json
from typing import Any, Dict, Optional
from enum import Enum

class CompressionType(Enum):
    ZLIB = 'zlib'
    LZMA = 'lzma'
    NONE = 'none'

class DataCompressor:
    def __init__(self, compression_type: CompressionType = CompressionType.ZLIB):
        self.compression_type = compression_type
        self._compressors = {
            CompressionType.ZLIB: self._zlib_compress,
            CompressionType.LZMA: self._lzma_compress,
            CompressionType.NONE: self._no_compress
        }
        self._decompressors = {
            CompressionType.ZLIB: self._zlib_decompress,
            CompressionType.LZMA: self._lzma_decompress,
            CompressionType.NONE: self._no_decompress
        }

    def compress(self, data: Dict[str, Any]) -> bytes:
        json_data = json.dumps(data).encode('utf-8')
        return self._compressors[self.compression_type](json_data)

    def decompress(self, data: bytes) -> Dict[str, Any]:
        json_data = self._decompressors[self.compression_type](data)
        return json.loads(json_data.decode('utf-8'))

    def _zlib_compress(self, data: bytes) -> bytes:
        return zlib.compress(data, level=9)

    def _zlib_decompress(self, data: bytes) -> bytes:
        return zlib.decompress(data)

    def _lzma_compress(self, data: bytes) -> bytes:
        return lzma.compress(data, preset=9)

    def _lzma_decompress(self, data: bytes) -> bytes:
        return lzma.decompress(data)

    def _no_compress(self, data: bytes) -> bytes:
        return data

    def _no_decompress(self, data: bytes) -> bytes:
        return data

    @property 
    def compression_ratio(self) -> float:
        """Retorna taxa de compressão média"""
        if not hasattr(self, '_total_original') or not self._total_original:
            return 1.0
        return self._total_compressed / self._total_original