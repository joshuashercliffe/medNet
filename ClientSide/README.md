To setup configured Node:
1. Power on node 
2. Enable monit with
> monit -d 1
3. Check monit status with 
> monit summary 
4. Check to see what other nodes are connected 
> curl -s localhost:26657/net_info | jq ".result.peers[].node_info | {id, listen_addr, moniker}"