import asyncio
import json
from typing import Dict, Set, Optional, Callable
import socket
import struct
import time

class NetworkDiscovery:
    def __init__(self, port: int = 5353, broadcast_addr: str = '255.255.255.255'):
        self.port = port
        self.broadcast_addr = broadcast_addr
        self._peers: Set[str] = set()
        self._callbacks: Dict[str, Callable] = {}
        self._running = False
        
    async def start(self):
        self._running = True
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
        self.socket.bind(('', self.port))
        
        asyncio.create_task(self._broadcast_loop())
        asyncio.create_task(self._listen_loop())
        
    async def stop(self):
        self._running = False
        self.socket.close()
        
    def on_peer_discovered(self, callback: Callable[[str, dict], None]):
        self._callbacks['discovered'] = callback
        
    async def _broadcast_loop(self):
        while self._running:
            try:
                announcement = {
                    'type': 'announce',
                    'id': id(self),
                    'timestamp': time.time()
                }
                self.socket.sendto(
                    json.dumps(announcement).encode(),
                    (self.broadcast_addr, self.port)
                )
                await asyncio.sleep(5)
            except Exception as e:
                print(f"Broadcast error: {e}")
                await asyncio.sleep(1)
                
    async def _listen_loop(self):
        while self._running:
            try:
                data, addr = self.socket.recvfrom(1024)
                message = json.loads(data.decode())
                
                if message['type'] == 'announce':
                    peer_id = message['id']
                    if peer_id not in self._peers:
                        self._peers.add(peer_id)
                        if callback := self._callbacks.get('discovered'):
                            callback(peer_id, {'address': addr[0]})
                            
                elif message['type'] == 'response':
                    # Processar respostas
                    pass
                    
            except Exception as e:
                print(f"Listen error: {e}")
                continue