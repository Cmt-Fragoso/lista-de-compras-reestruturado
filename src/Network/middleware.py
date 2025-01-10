from dataclasses import dataclass
from typing import Optional, Dict, Any
import logging
import time

@dataclass
class NetworkContext:
    timestamp: float
    operation: str
    peer_id: str
    data: Dict[str, Any]
    error_type: Optional[str] = None
    stack_trace: Optional[str] = None

class NetworkErrorMiddleware:
    def __init__(self):
        self.logger = logging.getLogger('network.middleware')
        self._recovery_states: Dict[str, Any] = {}
        
    def capture_state(self, peer_id: str, state: Dict[str, Any]) -> None:
        self._recovery_states[peer_id] = {
            'state': state.copy(),
            'timestamp': time.time()
        }

    def restore_state(self, peer_id: str) -> Optional[Dict[str, Any]]:
        if peer_id in self._recovery_states:
            return self._recovery_states[peer_id]['state']
        return None

    def handle_error(self, context: NetworkContext) -> bool:
        try:
            self.logger.error(
                f"Network error in {context.operation} "
                f"for peer {context.peer_id}: {context.error_type}"
            )

            if previous_state := self.restore_state(context.peer_id):
                context.data.update(previous_state)
                return True

            if context.error_type == "ConnectionError":
                return self._handle_connection_error(context)
            elif context.error_type == "SyncError": 
                return self._handle_sync_error(context)
            elif context.error_type == "DataError":
                return self._handle_data_error(context)
                
            return False
            
        except Exception as e:
            self.logger.critical(
                f"Failed to handle error: {str(e)}", 
                exc_info=True
            )
            return False

    def _handle_connection_error(self, context: NetworkContext) -> bool:
        return True

    def _handle_sync_error(self, context: NetworkContext) -> bool:
        return True

    def _handle_data_error(self, context: NetworkContext) -> bool:
        return True