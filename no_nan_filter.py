#!/usr/bin/env python
import rospy
from math import isnan
from sensor_msgs.msg import LaserScan

pub_scanFiltered = rospy.Publisher("scan_filtered", LaserScan, queue_size=10)

def scanCb(msg):
    #print('In scanCb')
    out = range(len(msg.ranges))
    for i,v in enumerate(msg.ranges):
        if isnan(v):
            out[i] = 255
        else:
            out[i] = v

    filteredScan = msg
    filteredScan.ranges = out
    pub_scanFiltered.publish(filteredScan)



def main():
    rospy.init_node("no_nan_filter", anonymous=False)

    rospy.Subscriber("scan", LaserScan, scanCb)

    rospy.sleep(1)

    rospy.spin()
    print('Exiting normally')

if __name__ == '__main__':
    main()
