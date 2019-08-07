# MediaProcessor2
A wrapper for FFEMPG and MEDIAINFO designed for encoding media for adaptive streaming. 

Current state: incomplete. 

  Currently implmented:
  * HEVC and H264 via FFEMPG
  * HDR and SDR encoding
  * Combing and telecine detection
  * Letter and pillar-box detection
  * Audtomated cropping

Audio support is not implemented.
To work with (for example) Azure Media Services ISM manifest generation is needed.
Aditionally to work at-scale the output media needs to be pre-fragmented and optimized for AMS.
